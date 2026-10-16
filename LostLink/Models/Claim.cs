// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: Claim.cs
//
// Purpose: Represents ownership claims and the processing details of such claims.
// ============================================================

using System;

namespace LostLink.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }

        public int? LostItemId { get; set; }

        public int FoundItemId { get; set; }

        // Retained for compatibility with older claims.
        public int? ClaimantUserId { get; set; }

        // Student who is claiming the found item.
        public string ClaimantName { get; set; } = string.Empty;

        public string VerificationNotes { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        // Staff member who recorded the claim.
        public int? SubmittedBy { get; set; }

        // Administrator who approved or rejected it.
        public int? ProcessedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ProcessedAt { get; set; }

        public DateTime? CollectedAt { get; set; }
    }
}
