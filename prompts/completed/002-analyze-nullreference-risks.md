<objective>
Thoroughly analyze the entire IPScan solution (both GUI and Core projects) to identify potential NullReferenceExceptions before they occur in production. This analysis will prevent crashes and improve application stability by proactively adding defensive null checks where needed.

The recent theme implementation exposed a pattern of WPF control initialization timing issues. We need to systematically find and fix similar risks across the codebase.
</objective>

<context>
This is a .NET 10.0 WPF application for network device scanning with two projects:
- IPScan.GUI: WPF user interface with windows, controls, and dialogs
- IPScan.Core: Business logic, models, and services

Recent NullReferenceExceptions fixed:
1. MainWindow.xaml.cs - `_settingsService` accessed before initialization
2. SettingsWindow.xaml.cs - `ColorPickerBorder` accessed during XAML loading
3. ColorPickerControl.xaml.cs - `ValidationText` and `PreviewBrush` accessed in event handlers during control construction

Common patterns to look for:
- Event handlers firing during XAML initialization before controls are fully loaded
- Service/field access before constructor completion
- Dependency injection timing issues
- Async operations accessing disposed or uninitialized objects
- Collection enumeration without null checks
- Property access chains without null-conditional operators
</context>

<analysis_requirements>
Thoroughly analyze all C# files in both projects for potential null reference risks:

1. **WPF Initialization Timing Issues** (HIGH PRIORITY):
   - Event handlers (SelectionChanged, TextChanged, ValueChanged, etc.) that may fire during XAML parsing
   - Control references used before InitializeComponent() completes
   - Dependency properties accessed in constructors
   - Resource dictionary lookups that may return null

2. **Service and Field Access**:
   - Fields/properties accessed before initialization in constructors
   - Dependency injection scenarios where services might be null
   - Async/await patterns with potential race conditions
   - Event subscriptions where the target may be null

3. **Collection and LINQ Operations**:
   - Collection enumeration without null checks
   - LINQ queries on potentially null collections
   - Dictionary/list indexing without ContainsKey/bounds checks

4. **Property Access Chains**:
   - Chained property access without null-conditional operators (e.g., `obj.Prop1.Prop2` instead of `obj?.Prop1?.Prop2`)
   - Cast operations without null checks
   - String operations on potentially null strings

5. **Async Patterns**:
   - Accessing UI controls from async continuations after window closure
   - Task results accessed without null checks
   - CancellationToken scenarios

For maximum efficiency, when examining multiple files, invoke Read tools for all files simultaneously rather than sequentially.
</analysis_requirements>

<execution_steps>
1. **Scan GUI Project Files**:
   - Read all .xaml.cs files in src/IPScan.GUI/
   - Read all custom control files in src/IPScan.GUI/Controls/
   - Identify patterns matching the known issues

2. **Scan Core Project Files**:
   - Read all service implementations in src/IPScan.Core/Services/
   - Read all model files in src/IPScan.Core/Models/
   - Check for null reference risks in business logic

3. **Categorize Findings**:
   - CRITICAL: Will definitely crash (same pattern as previous bugs)
   - HIGH: Likely to crash under common scenarios
   - MEDIUM: Could crash in edge cases
   - LOW: Defensive programming improvements

4. **Implement Preventive Fixes**:
   - Add null checks to event handlers that access controls
   - Add null-conditional operators where appropriate
   - Add defensive null checks before collection operations
   - Document WHY each check is needed in comments

5. **Generate Report**:
   - List all issues found with file:line references
   - Categorize by severity
   - Document which issues were fixed
   - Note any issues requiring architectural changes
</execution_steps>

<output>
1. **Analysis Report**: Save comprehensive findings to `./analyses/nullreference-analysis.md` with:
   - Executive summary with count by severity
   - Detailed findings organized by project and file
   - Pattern analysis (what types of issues are most common)
   - Recommendations for preventing future issues

2. **Code Fixes**: Modify C# files to add preventive null checks:
   - Use Edit tool to add null guards
   - Add inline comments explaining WHY each null check is needed
   - Follow the pattern from previous fixes (early return if null)
   - Preserve existing code style and formatting

3. **Summary**: After all fixes, provide a concise summary stating:
   - Total issues found
   - Issues fixed automatically
   - Issues requiring manual review
   - Recommended next steps
</output>

<fix_patterns>
Use these defensive patterns when implementing fixes:

**Event Handlers (WPF controls may be null during initialization)**:
```csharp
private void SomeControl_Changed(object sender, EventArgs e)
{
    // Control may not be initialized yet during window construction
    if (SomeControl == null) return;

    // ... rest of handler
}
```

**Field/Service Access**:
```csharp
private void MethodUsingService()
{
    // Service must be initialized before use
    if (_service == null)
    {
        // Log or throw meaningful error
        return;
    }

    // ... use service
}
```

**Property Access Chains**:
```csharp
// Before: user.Profile.Settings.Theme
// After:  user?.Profile?.Settings?.Theme ?? defaultTheme
```

**Collection Operations**:
```csharp
// Before: collection.FirstOrDefault()
// After:  collection?.FirstOrDefault()
```
</fix_patterns>

<verification>
Before declaring complete, verify:

1. All .xaml.cs files in GUI project have been examined
2. All service and model files in Core project have been examined
3. Analysis report is comprehensive and well-organized
4. All CRITICAL and HIGH severity issues have fixes implemented
5. Each null check has a comment explaining WHY it's needed
6. No existing functionality is broken by defensive checks
7. File:line references in report are accurate

After implementing fixes, consider running the application to ensure no regressions were introduced.
</verification>

<success_criteria>
- Comprehensive analysis report saved to ./analyses/nullreference-analysis.md
- All CRITICAL severity null reference risks have preventive fixes implemented
- All HIGH severity risks have fixes or documented reasons why fix wasn't appropriate
- Each fix includes explanatory comment
- Report includes accurate file:line references for all findings
- Summary provided with counts and next steps
- No breaking changes introduced by defensive null checks
</success_criteria>
