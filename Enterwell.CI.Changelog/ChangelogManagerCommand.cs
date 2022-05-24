﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Enterwell.CI.Changelog.Shared;
using Enterwell.CI.Changelog.ValidationRules;
using McMaster.Extensions.CommandLineUtils;

namespace Enterwell.CI.Changelog
{
    /// <summary>
    /// Entry point to the Changelog Manager CLI application.
    /// </summary>
    [Command(Name = "clm",
            FullName = "Changelog Manager CLI Tool",
            Description = "A CLI tool that uses files in the `changes` folder to fill out the `CHANGELOG.md` file.")]
    [VersionOptionFromMember("-v|--version", MemberName = "GetVersion")]
    public class ChangelogManagerCommand
    {
        private readonly ChangeGatheringService changeGatheringService;
        private readonly MarkdownTextService markdownTextService;
        private readonly FileWriterService fileWriterService;

        /// <summary>
        /// Initializes a new instance of the <c>FillingChangelogService</c> class.
        /// </summary>
        /// <param name="changeGatheringService">Service that knows how to gather changes from the changes location.</param>
        /// <param name="markdownTextService">Service that knows how to format changes into text for writing.</param>
        /// <param name="fileWriterService">Service that knows how to write changelog section into a file.</param>
        public ChangelogManagerCommand(ChangeGatheringService changeGatheringService, MarkdownTextService markdownTextService, FileWriterService fileWriterService)
        {
            this.changeGatheringService = changeGatheringService;
            this.markdownTextService = markdownTextService;
            this.fileWriterService = fileWriterService;
        }

        /// <summary>
        /// First argument to the CLI application representing the changelog location path.
        /// </summary>
        [Argument(0, "Changelog Location", "Directory location containing the `CHANGELOG.md`, and possibly, the configuration `.changelog.json` file.")]
        [DirectoryExists]
        [Required]
        public string ChangelogLocation { get; set; }

        /// <summary>
        /// Second argument to the CLI application representing the changes location path.
        /// </summary>
        [Argument(1, "Changes location", "Directory location containing the changes.")]
        [DirectoryExists]
        [Required]
        public string ChangesLocation { get; set; }

        /// <summary>
        /// Optional parameter to the CLI application representing if the new application's version should be set in the appropriate project file.
        /// </summary>
        [Option("-sv|--set-version[:<PROJECT_FILE_PATH>]",
            Description = "Should the new application's version be set in the appropriate project file. If set without a file path, application will try to automatically determine the project file. " +
                          "Currently supported project types: NPM (package.json) and .NET SDK (*.csproj with the version tag).")]
        [ValidateProjectFile]
        public (bool isEnabled, string projectFilePath) SetVersion { get; set; }

        /// <summary>
        /// Main method of the application. The method delegates all the work to the appropriate services.
        /// </summary>
        /// <returns>An asynchronous task.</returns>
        public async Task OnExecute()
        {
            var logger = new ConsoleLogger();

            try
            {
                // Gathering changes and the bumping the version
                var versionInformation = await this.changeGatheringService.GatherVersionInformation(this.ChangelogLocation, this.ChangesLocation);

                // If the set-version flag is set
                if (this.SetVersion.isEnabled)
                {
                    await this.fileWriterService.BumpProjectFileVersion(versionInformation.SemanticVersion, this.SetVersion.projectFilePath);
                }

                // Building the string representing the new changelog section for the new bumped version
                var newChangelogSection = this.markdownTextService.BuildChangelogSection(versionInformation);
                var elementToInsertChangelogSectionBefore = this.markdownTextService.ToH2(string.Empty);

                // Write the newly built section to the changelog
                await this.fileWriterService.WriteToChangelog(newChangelogSection, this.ChangelogLocation, elementToInsertChangelogSectionBefore);

                // Delete the accepted change files
                this.changeGatheringService.RemoveAcceptedChanges(versionInformation.Changes);

                logger.LogSuccess(versionInformation.SemanticVersion);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.Message);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Reads the application version from the .csproj file.
        /// </summary>
        /// <returns><see cref="string"/> representing the application version.</returns>
        private string? GetVersion()
        {
            return typeof(ChangelogManagerCommand)
                .Assembly?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
        }
    }
}