# MockAgentSample

動きのイメージ

ユーザー入力:

``` text
売上CSVを読んで、異常値があれば要約してSlackに送って
```
内部では、

- ダミーAIが「CSVを読む必要あり」と判断
- ReadSalesCsv() 実行
- ダミーAIが「AnalyzeSales() を呼ぶ」と判断
- AnalyzeSales() 実行
- 異常値があればダミーAIが「PostToSlack() を呼ぶ」と判断
- PostToSlack() 実行
- 最終メッセージを返す

あとで本物AIに差し替える場所

## 差し替えるのは主にここ

``` csharp
public class MockAiPlanner
{
    public AgentDecision DecideNextStep(AgentContext context)
```

今はここが固定ルールで

- CSV未読なら ReadSalesCsv
- 未分析なら AnalyzeSales
- 異常があれば PostToSlack
- 終了

本物にすると、ここでモデルに

- 現在の状態
- 利用可能ツール一覧
- ユーザー依頼

を渡して、
「次にどのツールを呼ぶか」を返させる形。