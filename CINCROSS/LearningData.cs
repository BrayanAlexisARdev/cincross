using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CINCROSS;

public class SubjectData
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ColorHex { get; set; } = "#FFFFFF";
    public string IconChar { get; set; } = "?";

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastStudyDate { get; set; }
    public int TotalXp { get; set; }
    public int Level { get; set; } = 1;

    public DateTime TodayDate { get; set; }
    public int TodayMinutesStudied { get; set; }
    public int TodayXpEarned { get; set; }

    public int GetXpForNextLevel()
    {
        return Level * 1000;
    }

    public void CompleteSession(int minutes, int quizXp = 0)
    {
        int xp = (minutes * 10) + quizXp;
        DateTime now = DateTime.Today;

        if (LastStudyDate.HasValue)
        {
            TimeSpan diff = now - LastStudyDate.Value;
            if (diff.TotalDays == 1)
                CurrentStreak++;
            else if (diff.TotalDays > 1)
                CurrentStreak = 1;
        }
        else
        {
            CurrentStreak = 1;
        }

        if (CurrentStreak > LongestStreak)
            LongestStreak = CurrentStreak;

        LastStudyDate = now;

        if (TodayDate == now)
        {
            TodayMinutesStudied += minutes;
            TodayXpEarned += xp;
        }
        else
        {
            TodayDate = now;
            TodayMinutesStudied = minutes;
            TodayXpEarned = xp;
        }

        TotalXp += xp;
        Level = (TotalXp / 1000) + 1;
    }
}

public class LearningData
{
    public int DailyGoalMinutes { get; set; } = 20;
    public string LastActiveSubject { get; set; } = "MATH";
    public int ColorSchemeIndex { get; set; }
    public List<SubjectData> Subjects { get; set; } = new();

    public static string GetFilePath()
    {
        return Path.Combine(PathHelper.GetFilesDir(), "learning_data.json");
    }

    public static LearningData? Load()
    {
        try
        {
            string path = GetFilePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<LearningData>(json);
            }
        }
        catch (Exception ex) { Logger.Error("LearningData.Load", ex); }
        return null;
    }

    public void Save()
    {
        try
        {
            string path = GetFilePath();
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex) { Logger.Error("LearningData.Save", ex); }
    }

    public static LearningData CreateDefault()
    {
        return new LearningData
        {
            Subjects = new List<SubjectData>
            {
                new() { Name = "MATH", DisplayName = "Matemáticas", ColorHex = "#1E90FF", IconChar = "M" },
                new() { Name = "SQL",  DisplayName = "SQL",           ColorHex = "#FF8C00", IconChar = "S" },
                new() { Name = "PROG", DisplayName = "Programación",  ColorHex = "#58CC02", IconChar = "P" },
                new() { Name = "ENG",  DisplayName = "Inglés",        ColorHex = "#FF4444", IconChar = "E" },
            }
        };
    }
}
