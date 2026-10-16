// ============================================================
// CSP3341 - Programming Languages Assignment 2
// Project: LostLink - Smart Campus Lost and Found Management
//
// Author: Chenuli Alwis
// Student ID: 10711140
// File: OptionalImageConverter.cs
//
// Purpose: Converts paths for optional item images into displayable paths.
// ============================================================

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LostLink
{
    /// <summary>
    /// Converts an optional image path to an ImageSource.
    /// A missing or invalid image returns null, allowing the existing
    /// "No photo" placeholder to remain visible without binding errors.
    /// </summary>
    public sealed class OptionalImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return new ImageSourceConverter()
                    .ConvertFromString(null, culture, path) as ImageSource;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Image binding is one-way only.");
        }
    }
}
