using LiteDB;
using System;
using System.IO;

public static class DatabaseService
{
    private static string _dbPath;

    public static LiteDatabase GetConnection()
    {
        // Путь: C:\Users\Имя\AppData\Local\GitDiscussionsApp\data.db
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "GitDiscussionsApp");

        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        _dbPath = Path.Combine(folder, "discussions.db");
        return new LiteDatabase(_dbPath);
    }
}
