﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.Editor
{
    public sealed class EditorSettingsChangedEventArgs : EventArgs
    {
        public EditorSettingsChangedEventArgs(EditorSettings settings)
        {
            Settings = settings;
        }

        public EditorSettings Settings { get; }
    }
}
