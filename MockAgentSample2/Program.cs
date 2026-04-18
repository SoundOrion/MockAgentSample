using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MockAgentSample;

public class Program
{
    public static void Main(string[] args)
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "sales.csv");

        EnsureSampleCsvExists(csvPath);

        var userRequest = "売上CSVを読んで、異常値があれば要約してSlackに送って";

        var agent = new MockAgentOrchestrator(
            new MockAiPlanner(),
            new SalesTools(),
            new SlackTools()
        );

        var result = agent.Run(userRequest, csvPath);

        Console.WriteLine();
        Console.WriteLine("===== FINAL RESULT =====");
        Console.WriteLine(result);
    }

    private static void EnsureSampleCsvExists(string csvPath)
    {
        if (File.Exists(csvPath))
        {
            return;
        }

        File.WriteAllText(csvPath,
@"Date,Store,Sales
2026-04-01,Tokyo,1200
2026-04-02,Tokyo,1180
2026-04-03,Tokyo,1220
2026-04-04,Tokyo,1190
2026-04-05,Tokyo,1210
2026-04-06,Tokyo,5000
2026-04-07,Tokyo,1175");
    }
}

public class SalesRecord
{
    public DateTime Date { get; set; }
    public string Store { get; set; } = "";
    public decimal Sales { get; set; }
}

public class SalesAnalysisResult
{
    public bool HasAnomalies { get; set; }
    public List<SalesRecord> Anomalies { get; set; } = new();
    public decimal AverageSales { get; set; }
    public string Summary { get; set; } = "";
}

public class AgentContext
{
    public string UserRequest { get; set; } = "";
    public string CsvPath { get; set; } = "";

    public bool CsvLoaded { get; set; }
    public bool AnalysisCompleted { get; set; }
    public bool SlackPosted { get; set; }

    public List<SalesRecord>? SalesRecords { get; set; }
    public SalesAnalysisResult? AnalysisResult { get; set; }
}

public class AgentDecision
{
    public string ActionType { get; private set; } = "";
    public string ToolName { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public string FinalMessage { get; private set; } = "";

    public static AgentDecision CallTool(string toolName, string reason)
    {
        return new AgentDecision
        {
            ActionType = "tool",
            ToolName = toolName,
            Reason = reason
        };
    }

    public static AgentDecision Finish(string finalMessage)
    {
        return new AgentDecision
        {
            ActionType = "finish",
            FinalMessage = finalMessage
        };
    }
}

public class SalesTools
{
    public List<SalesRecord> ReadSalesCsv(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("CSV file not found.", csvPath);
        }

        var lines = File.ReadAllLines(csvPath).Skip(1);
        var records = new List<SalesRecord>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length != 3)
            {
                continue;
            }

            records.Add(new SalesRecord
            {
                Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
                Store = parts[1],
                Sales = decimal.Parse(parts[2], CultureInfo.InvariantCulture)
            });
        }

        Console.WriteLine($"[Tool] ReadSalesCsv: {records.Count} records loaded.");
        return records;
    }

    public SalesAnalysisResult AnalyzeSales(List<SalesRecord> records)
    {
        if (records.Count == 0)
        {
            return new SalesAnalysisResult
            {
                HasAnomalies = false,
                AverageSales = 0,
                Summary = "データがありません。"
            };
        }

        var average = records.Average(x => x.Sales);

        // 単純な異常値判定
        // 平均の1.8倍超 or 平均の0.5倍未満
        var anomalies = records
            .Where(x => x.Sales > average * 1.8m || x.Sales < average * 0.5m)
            .ToList();

        var summary = anomalies.Count == 0
            ? $"売上を分析しました。平均売上は {average:F2} で、異常値は見つかりませんでした。"
            : $"売上を分析しました。平均売上は {average:F2} で、異常値は {anomalies.Count} 件見つかりました。";

        Console.WriteLine($"[Tool] AnalyzeSales: avg={average:F2}, anomalies={anomalies.Count}");

        return new SalesAnalysisResult
        {
            HasAnomalies = anomalies.Count > 0,
            Anomalies = anomalies,
            AverageSales = average,
            Summary = summary
        };
    }
}

public class SlackTools
{
    public void PostToSlack(string channel, string message)
    {
        // 本物のSlack送信の代わりにコンソール出力
        Console.WriteLine($"[Tool] PostToSlack -> channel: {channel}");
        Console.WriteLine("--------- Slack Message ---------");
        Console.WriteLine(message);
        Console.WriteLine("---------------------------------");
    }
}

public class MockAiPlanner
{
    public AgentDecision DecideNextStep(AgentContext context)
    {
        if (!context.CsvLoaded)
        {
            return AgentDecision.CallTool(
                "ReadSalesCsv",
                "売上CSVを読む必要があるため");
        }

        if (!context.AnalysisCompleted)
        {
            return AgentDecision.CallTool(
                "AnalyzeSales",
                "異常値の有無を確認するため");
        }

        if (context.AnalysisResult?.HasAnomalies == true && !context.SlackPosted)
        {
            return AgentDecision.CallTool(
                "PostToSlack",
                "異常値が見つかったためSlack通知する");
        }

        return AgentDecision.Finish(BuildFinalMessage(context));
    }

    private string BuildFinalMessage(AgentContext context)
    {
        if (context.AnalysisResult == null)
        {
            return "分析は完了しませんでした。";
        }

        if (!context.AnalysisResult.HasAnomalies)
        {
            return context.AnalysisResult.Summary + " Slack送信は不要でした。";
        }

        if (context.SlackPosted)
        {
            return context.AnalysisResult.Summary + " Slackに通知しました。";
        }

        return context.AnalysisResult.Summary + " ただしSlack送信は未実施です。";
    }
}

public class MockAgentOrchestrator
{
    private readonly MockAiPlanner _planner;
    private readonly SalesTools _salesTools;
    private readonly SlackTools _slackTools;

    public MockAgentOrchestrator(
        MockAiPlanner planner,
        SalesTools salesTools,
        SlackTools slackTools)
    {
        _planner = planner;
        _salesTools = salesTools;
        _slackTools = slackTools;
    }

    public string Run(string userRequest, string csvPath)
    {
        var context = new AgentContext
        {
            UserRequest = userRequest,
            CsvPath = csvPath
        };

        for (int step = 1; step <= 10; step++)
        {
            var decision = _planner.DecideNextStep(context);

            Console.WriteLine();
            Console.WriteLine($"[Agent] Step {step}");
            Console.WriteLine($"[Agent] Decision: {decision.ActionType}");

            if (decision.ActionType == "finish")
            {
                Console.WriteLine("[Agent] Finished.");
                return decision.FinalMessage;
            }

            Console.WriteLine($"[Agent] Tool: {decision.ToolName}");
            Console.WriteLine($"[Agent] Reason: {decision.Reason}");

            ExecuteTool(decision.ToolName, context);
        }

        return "ステップ数上限に達したため終了しました。";
    }

    private void ExecuteTool(string toolName, AgentContext context)
    {
        switch (toolName)
        {
            case "ReadSalesCsv":
                context.SalesRecords = _salesTools.ReadSalesCsv(context.CsvPath);
                context.CsvLoaded = true;
                break;

            case "AnalyzeSales":
                if (context.SalesRecords == null)
                {
                    throw new InvalidOperationException("CSVがまだ読み込まれていません。");
                }

                context.AnalysisResult = _salesTools.AnalyzeSales(context.SalesRecords);
                context.AnalysisCompleted = true;
                break;

            case "PostToSlack":
                if (context.AnalysisResult == null)
                {
                    throw new InvalidOperationException("分析結果がありません。");
                }

                var slackMessage = BuildSlackMessage(context.AnalysisResult);
                _slackTools.PostToSlack("#sales-alerts", slackMessage);
                context.SlackPosted = true;
                break;

            default:
                throw new NotSupportedException($"Unknown tool: {toolName}");
        }
    }

    private string BuildSlackMessage(SalesAnalysisResult result)
    {
        var lines = new List<string>
        {
            "売上異常検知アラート",
            result.Summary
        };

        foreach (var anomaly in result.Anomalies)
        {
            lines.Add($"- {anomaly.Date:yyyy-MM-dd} / {anomaly.Store} / Sales={anomaly.Sales}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}