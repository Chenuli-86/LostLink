// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: MatchingService.cs
//
// Purpose: Recognizes and estimates potential matches for a lost item.
// ============================================================

using LostLink.Enums;
using LostLink.Models;

namespace LostLink.Services
{
    public class MatchingService
    {
        /// <summary>
        /// Compares one lost item with one found item and
        /// calculates a rule-based LostLink match score.
        /// </summary>
        public MatchResult CalculateMatch(Item lostItem, Item foundItem)
        {
            bool categoryMatched =
                TextMatches(lostItem.Category, foundItem.Category);

            bool colourMatched =
                TextMatches(lostItem.Colour, foundItem.Colour);

            bool locationMatched =
                TextMatches(lostItem.Location, foundItem.Location);


            double dateScore =
                CalculateDateScore(
                    lostItem.IncidentDate,
                    foundItem.IncidentDate);

            List<string> matchedKeywords =
                FindMatchedKeywords(
                    lostItem.Description,
                    foundItem.Description);

            double keywordScore =
                CalculateKeywordScore(
                    lostItem.Description,
                    matchedKeywords);

            // Weighted score out of 100.
            double totalScore = 0;

            if (categoryMatched)
                totalScore += 30;

            if (colourMatched)
                totalScore += 20;

            if (locationMatched)
                totalScore += 20;

            totalScore += dateScore * 15;
            totalScore += keywordScore * 15;

            totalScore =
                Math.Round(
                    Math.Min(totalScore, 100),
                    2);

            MatchClassification classification =
                ClassifyMatch(totalScore);

            return new MatchResult
            {
                LostItemId = lostItem.ItemId,
                FoundItemId = foundItem.ItemId,
                MatchScore = totalScore,
                Classification = classification,
                CategoryMatched = categoryMatched,
                ColourMatched = colourMatched,
                LocationMatched = locationMatched,
                DateScore = Math.Round(dateScore, 2),
                KeywordScore = Math.Round(keywordScore, 2),
                MatchedKeywords = string.Join(
                    ", ",
                    matchedKeywords),
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Compares two item attributes.
        /// Missing or blank values must never count as a match.
        /// </summary>
        private static bool TextMatches(
            string first,
            string second)
        {
            if (string.IsNullOrWhiteSpace(first) ||
                string.IsNullOrWhiteSpace(second))
            {
                return false;
            }

            return string.Equals(
                first.Trim(),
                second.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gives a higher score when the lost and found dates
        /// are close together.
        /// </summary>
        private static double CalculateDateScore(
            DateTime lostDate,
            DateTime foundDate)
        {
            double daysApart =
                Math.Abs(
                    (foundDate.Date - lostDate.Date).TotalDays);

            if (daysApart == 0)
                return 1.0;

            if (daysApart <= 1)
                return 0.8;

            if (daysApart <= 3)
                return 0.5;

            if (daysApart <= 7)
                return 0.2;

            return 0.0;
        }

        /// <summary>
        /// Finds meaningful words that occur in both descriptions.
        /// </summary>
        private static List<string> FindMatchedKeywords(
            string lostDescription,
            string foundDescription)
        {
            HashSet<string> lostWords =
                ExtractKeywords(lostDescription);

            HashSet<string> foundWords =
                ExtractKeywords(foundDescription);

            return lostWords
                .Intersect(
                    foundWords,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(word => word)
                .ToList();
        }

        /// <summary>
        /// Converts a description into a set of useful keywords.
        /// </summary>
        private static HashSet<string> ExtractKeywords(
            string description)
        {
            char[] separators =
            {
                ' ', ',', '.', ';', ':',
                '-', '_', '/', '\\',
                '!', '?', '(', ')'
            };

            return description
                .ToLowerInvariant()
                .Split(
                    separators,
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length >= 3)
                .ToHashSet();
        }

        /// <summary>
        /// Calculates how much of the lost-item description
        /// overlaps with the found-item description.
        /// </summary>
        private static double CalculateKeywordScore(
            string lostDescription,
            List<string> matchedKeywords)
        {
            HashSet<string> lostWords =
                ExtractKeywords(lostDescription);

            if (lostWords.Count == 0)
                return 0;

            return Math.Min(
                (double)matchedKeywords.Count /
                lostWords.Count,
                1.0);
        }

        /// <summary>
        /// Converts the numeric score into a LostLink
        /// match classification.
        /// </summary>
        private static MatchClassification ClassifyMatch(
            double score)
        {
            if (score >= 75)
                return MatchClassification.Strong;

            if (score >= 45)
                return MatchClassification.Possible;

            return MatchClassification.Low;
        }
    }
}