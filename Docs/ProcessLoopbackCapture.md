# Process Loopback Capture（プロセス別ループバックキャプチャ）

WASAPI の Process Loopback を使うと、特定プロセスが再生する音声だけをキャプチャできます。  
`WasapiCapture.CreateForProcessCaptureAsync(processId, includeProcessTree)` でインスタンスを取得し、`StartRecording()` でキャプチャを開始します。

**重要**: Process Loopback は COM が STA（通常は UI スレッド）にバインドされている必要があります。スレッドや SynchronizationContext を誤ると、`E_NOINTERFACE` や「実音が取れず -1/0/1 だけのプレースホルダー」になります。

---

## スレッドと SynchronizationContext の要件

### どこで何が決まるか

| 項目 | 決まるタイミング | 条件 |
|------|------------------|------|
| **IAudioClient の STA スレッド** | `CreateForProcessCaptureAsync` の **await の継続が実行されるスレッド** | そのスレッドで `SynchronizationContext.Current` がキャプチャされる |
| **syncContext** | `WasapiCapture` のコンストラクタ（＝ await 継続内） | `SynchronizationContext.Current` の値が保存される |
| **StartRecording を呼ぶべきスレッド** | 同上 | **await 継続と同じスレッド**で呼ぶこと |

### 正しい呼び出し方（WPF の例）

```csharp
// UI スレッド（例: ボタンクリック）で開始
private async void StartButton_Click(object sender, RoutedEventArgs e)
{
    // ① このメソッドが UI スレッドで実行されている
    // ② await には ConfigureAwait(false) を付けない
    var capture = await WasapiCapture.CreateForProcessCaptureAsync(processId, includeProcessTree: false);

    // ③ 継続も UI スレッドで実行される → syncContext が UI のそれになる
    capture.DataAvailable += OnDataAvailable;
    capture.RecordingStopped += OnRecordingStopped;

    // ④ 同じ UI スレッドで StartRecording を呼ぶ
    capture.StartRecording();
}
```

- `CreateForProcessCaptureAsync` を **await しているスレッド** = その後の継続が実行されるスレッド（通常は UI スレッド）。
- そのスレッドの `SynchronizationContext.Current` が WasapiCapture 内部に保存され、キャプチャ中のすべての COM 呼び出しがそのコンテキストにポストされる。

### やってはいけないこと

- **await に `ConfigureAwait(false)` を付ける**  
  → 継続がスレッドプールで実行され、`SynchronizationContext.Current` が null になり、Process Loopback で COM が STA にバインドされない。

- **別スレッドで CreateForProcessCaptureAsync / StartRecording を呼ぶ**  
  例: `Task.Run(() => CreateForProcessCaptureAsync(...))` の結果を UI で受け取って `StartRecording()` する、など。  
  → インスタンスはスレッドプールのコンテキストで作られ、COM は STA にバインドされていない。

- **キャプチャ開始前に SynchronizationContext が変わる**  
  例: 「UI スレッドで一度何か await」したあと、別のサービスや VM のメソッドから `CreateForProcessCaptureAsync` を呼んでいて、その時点で `Current` が別物や null になっている。  
  → 正しい STA がキャプチャされない。

---

## 切り分けチェックリスト（E_NOINTERFACE / プレースホルダーだけのとき）

以下を順に確認すると原因を絞りやすいです。

1. **CreateForProcessCaptureAsync を await しているスレッド**
   - [ ] その処理は **UI スレッド**（または少なくとも STA で SynchronizationContext が設定されているスレッド）で実行されているか？
   - [ ] その await から **呼び出し元まで**、`ConfigureAwait(false)` は一切使っていないか？

2. **StartRecording() を呼んでいるスレッド**
   - [ ] **CreateForProcessCaptureAsync の await の直後の継続と同じスレッド**で呼んでいるか？（典型的には同じ async メソッド内で await の直後に `StartRecording()` を呼べば同じスレッド）

3. **SynchronizationContext**
   - [ ] キャプチャ開始（CreateForProcessCaptureAsync を呼ぶ時点）で、そのスレッドの `SynchronizationContext.Current` は null ではないか？（WPF の UI スレッドなら通常は非 null）
   - [ ] 「パイプライン開始」など別の async 処理のあとにキャプチャを開始していて、その時点で **Current が別のコンテキストや null に切り替わっていないか**？

4. **参照サンプル**
   - このリポジトリの **MinimalProcessLoopbackWpf**（`NAudio/MinimalProcessLoopbackWpf`）は、上記を満たした最小の WPF 例です。  
     同じプロセスを指定してそこで実音が取れるなら、問題は呼び出し側のスレッド／コンテキストにあります。

---

## コード例（最小 WPF）

```csharp
// 必ず UI スレッドで実行されるイベントから呼ぶ
private async void StartCapture_Click(object sender, RoutedEventArgs e)
{
    int processId = ...; // 対象プロセス ID

    // ConfigureAwait(false) は付けない
    var capture = await WasapiCapture.CreateForProcessCaptureAsync(processId, includeProcessTree: false);

    capture.DataAvailable += (s, a) => { /* ... */ };
    capture.RecordingStopped += (s, a) => { /* ... */ };

    // 同じ UI スレッドで即 StartRecording
    capture.StartRecording();
}
```

これで、`CreateForProcessCaptureAsync` も `StartRecording()` も **同じ UI スレッド**で呼ばれ、そのスレッドの SynchronizationContext がキャプチャ全体で使われます。
