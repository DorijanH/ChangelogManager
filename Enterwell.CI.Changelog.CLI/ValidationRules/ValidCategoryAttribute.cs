﻿using System.ComponentModel.DataAnnotations;
using System.IO;
using Enterwell.CI.Changelog.Shared;

namespace Enterwell.CI.Changelog.CLI.ValidationRules
{
    /// <summary>
    /// Specifies that the data must be valid according to the configuration file if one exists. Otherwise, everything is valid.
    /// </summary>
    public class ValidCategoryAttribute : ValidationAttribute
    {
        /// <summary>
        /// Custom validation attribute constructor. Passes the error message that will be displayed to the user if the validation fails to its base class.
        /// </summary>
        public ValidCategoryAttribute() : base("The change category is not valid based on the configuration file.") { }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var config = Configuration.LoadConfiguration(Directory.GetCurrentDirectory());
            
            // Any input data is valid if the configuration file does not exist or if its empty.
            if (config == null || config.IsEmpty())
            {
                return ValidationResult.Success;
            }
            else
            {
                var inputString = (string)value;

                if (config.IsValid(inputString?.Trim()))
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
            }
        }
    }
}