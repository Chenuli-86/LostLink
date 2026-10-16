// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: Item.cs
//
// Purpose: Represents lost and found items and their information.
// ============================================================

using LostLink.Enums;

namespace LostLink.Models
{
    public class Item
    {
        public int ItemId { get; set; }

        public string Title { get; set; } = string.Empty;

        public ItemType Type { get; set; }

        public string Category { get; set; } = string.Empty;

        public string Colour { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public DateTime IncidentDate { get; set; }

        public string Description { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        public ItemStatus Status { get; set; } = ItemStatus.Open;

        public int ReportedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}