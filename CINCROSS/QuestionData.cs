using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CINCROSS;

public class Question
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("correctAnswer")] public bool CorrectAnswer { get; set; }
    [JsonPropertyName("options")] public List<string> Options { get; set; } = new();
    [JsonPropertyName("correctIndex")] public int CorrectIndex { get; set; }
    [JsonPropertyName("points")] public int Points { get; set; } = 10;
}

public class QuestionLevel
{
    [JsonPropertyName("level")] public int Level { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("questions")] public List<Question> Questions { get; set; } = new();
}

public static class QuestionBank
{
    public static List<QuestionLevel>? Load(string subjectName)
    {
        try
        {
            string path = GetFilePath(subjectName);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<QuestionLevel>>(json);
        }
        catch { return null; }
    }

    private static string GetFilePath(string subjectName)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            string testPath = Path.Combine(baseDir, "files", "questions", $"{subjectName}.json");
            if (Directory.Exists(Path.Combine(baseDir, "files", "questions")))
                return testPath;
            var parent = Directory.GetParent(baseDir);
            if (parent == null) break;
            baseDir = parent.FullName;
        }
        return Path.Combine(baseDir, "files", "questions", $"{subjectName}.json");
    }

    public static List<Question> GetQuestionsForLevel(List<QuestionLevel> levels, int level)
    {
        var ql = levels.FirstOrDefault(l => l.Level == level);
        return ql?.Questions ?? new List<Question>();
    }

    public static List<Question> GetAllQuestions(List<QuestionLevel> levels)
    {
        return levels.SelectMany(l => l.Questions).ToList();
    }

    public static List<Question> GetRandomQuestions(List<QuestionLevel> levels, int count, int maxLevel)
    {
        var rng = new Random();
        var available = levels.Where(l => l.Level <= maxLevel).SelectMany(l => l.Questions).ToList();
        available = available.OrderBy(_ => rng.Next()).Take(Math.Min(count, available.Count)).ToList();
        return available;
    }
}
