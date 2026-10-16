// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: MatchResult.cs
//
// Purpose: Shows a possible correlation of lost and found items.
// ============================================================

using LostLink.Enums;

namespace LostLink.Models
{
    public class MatchResult
    {
        public int MatchId { get; set; }

        public int LostItemId { get; set; }

        public int FoundItemId { get; set; }

        public double MatchScore { get; set; }

        public MatchClassification Classification { get; set; }

        public bool CategoryMatched { get; set; }

        public bool ColourMatched { get; set; }

        public bool LocationMatched { get; set; }

        public double DateScore { get; set; }

        public double KeywordScore { get; set; }

        public string MatchedKeywords { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}