using System;

namespace Space_intruders
{
    public class ScoreEntry : IComparable<ScoreEntry>
    {
        public string Nickname { get; set; }
        public int Score { get; set; }
        public DateTime Timestamp { get; set; }

        public ScoreEntry() { }

        public ScoreEntry(string nickname, int score, DateTime timestamp)
        {
            Nickname = nickname;
            Score = score;
            Timestamp = timestamp;
        }

        public int CompareTo(ScoreEntry other)
        {
            if (other == null) return 1;

            int scoreComparison = other.Score.CompareTo(this.Score);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            return this.Timestamp.CompareTo(other.Timestamp);
        }

        public override string ToString()
        {
            return $"{Nickname}: {Score} ({Timestamp:yyyy-MM-dd HH:mm})";
        }
    }
}