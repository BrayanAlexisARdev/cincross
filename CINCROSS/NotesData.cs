using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CINCROSS;

/// <summary>Una nota guardada por el usuario (nombre + contenido).</summary>
public class NoteItem
{
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.Now;
}

/// <summary>Persistencia de las notas (máximo 10) en %LOCALAPPDATA%\CINCROSS\notes_data.json.</summary>
public class NotesData
{
    public const int MaxNotes = 10;

    public List<NoteItem> Notes { get; set; } = new();

    public static string GetFilePath()
    {
        return Path.Combine(PathHelper.GetDataDir(), "notes_data.json");
    }

    public static NotesData Load()
    {
        try
        {
            string path = GetFilePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                NotesData? data = JsonSerializer.Deserialize<NotesData>(json);
                if (data != null)
                {
                    data.Notes ??= new List<NoteItem>();
                    while (data.Notes.Count < MaxNotes)
                        data.Notes.Add(new NoteItem());
                    if (data.Notes.Count > MaxNotes)
                        data.Notes = data.Notes.GetRange(0, MaxNotes);
                    return data;
                }
            }
        }
        catch (Exception ex) { Logger.Error("NotesData.Load", ex); }

        NotesData fresh = new NotesData();
        for (int i = 0; i < MaxNotes; i++)
            fresh.Notes.Add(new NoteItem());
        return fresh;
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
        catch (Exception ex) { Logger.Error("NotesData.Save", ex); }
    }

    public static bool IsEmpty(NoteItem? note)
    {
        return note == null ||
            (string.IsNullOrWhiteSpace(note.Name) && string.IsNullOrWhiteSpace(note.Content));
    }
}
