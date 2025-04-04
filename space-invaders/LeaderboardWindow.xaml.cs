using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Space_intruders
{
    // Wrapper class to add Rank property for display
    public class LeaderboardDisplayEntry : ScoreEntry
    {
        public int Rank { get; set; }
    }

    public partial class LeaderboardWindow : Window
    {
        private static readonly string ScoreFileName = "scores.csv";
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpaceIntrudersMedieval"); // Your game's folder name

        private static readonly string ScoreFilePath = Path.Combine(AppDataFolder, ScoreFileName);

        public LeaderboardWindow()
        {
            InitializeComponent();
            LoadScores();
        }

        private void LoadScores()
        {
            List<ScoreEntry> scores = new List<ScoreEntry>();

            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(AppDataFolder);

                if (File.Exists(ScoreFilePath))
                {
                    string[] lines = File.ReadAllLines(ScoreFilePath);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split(','); // Using comma as separator
                        if (parts.Length == 3)
                        {
                            string nickname = parts[0];
                            // Use TryParse for robustness
                            if (int.TryParse(parts[1], out int score) &&
                                DateTime.TryParse(parts[2], null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp))
                            {
                                scores.Add(new ScoreEntry(nickname, score, timestamp));
                            }
                            else
                            {
                                Debug.WriteLine($"Skipping malformed score line: {line}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Skipping incorrectly formatted score line: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading scores: {ex.Message}");
                MessageBox.Show($"Could not load scores.\nError: {ex.Message}", "Leaderboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Sort scores (uses CompareTo defined in ScoreEntry)
            scores.Sort();

            // Take top 10 and add Rank for display
            var topScores = scores.Take(10)
                                 .Select((entry, index) => new LeaderboardDisplayEntry
                                 {
                                     Rank = index + 1,
                                     Nickname = entry.Nickname,
                                     Score = entry.Score,
                                     Timestamp = entry.Timestamp
                                 }).ToList();

            LeaderboardListView.ItemsSource = topScores;
        }
    }
}