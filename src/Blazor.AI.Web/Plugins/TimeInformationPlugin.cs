// ***********************************************************************
// Author           : glensouza
// Created          : 01-14-2025
//
// Last Modified By : glensouza
// Last Modified On : 01-16-2025
// ***********************************************************************
// <summary>This plugin provides a function to retrieve the current time in UTC.</summary>
// ***********************************************************************

using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Blazor.AI.Web.Plugins;

// ReSharper disable once ClassNeverInstantiated.Global
public class TimeInformationPlugin
{
    [KernelFunction]
    [Description("Retrieves the current time in UTC.")]
    // ReSharper disable once UnusedMember.Global
    public string GetCurrentUtcTime() => DateTime.UtcNow.ToString("R");
}
