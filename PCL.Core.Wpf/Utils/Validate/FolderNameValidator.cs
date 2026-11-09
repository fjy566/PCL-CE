using System;
using System.IO;
using System.Linq;
using FluentValidation;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.Validate;

public class FolderNameValidator : FileSystemValidator
{
    public FolderNameValidator() : this(null)
    {
    }

    public bool UseMinecraftCharCheck { get; set; }
    public bool IgnoreCase { get; set; }
    public bool IgnoreSameNameInParentFolder { get; set; }
    public string? ParentFolder { get; set; }

    public FolderNameValidator(string? parentFolder = null,
        bool useMinecraftCharCheck = true,
        bool ignoreCase = true,
        bool ignoreSameNameInParentFolder = false)
    {
        UseMinecraftCharCheck = useMinecraftCharCheck;
        IgnoreCase = ignoreCase;
        IgnoreSameNameInParentFolder = ignoreSameNameInParentFolder;
        ParentFolder = parentFolder;

        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.Empty"))
            .Must(x => !x.StartsWith(' '))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderName.StartsWithSpace"))
            .Must(x => !x.EndsWith(' '))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderName.EndsWithSpace"))
            .Must(x => !x.EndsWith('.'))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderName.EndsWithDot"))
            .Custom((fileName, context) =>
            {
                var invalidChar = CheckInvalidStrings(
                    fileName,
                    UseMinecraftCharCheck ? ["!;"] : []);
                if (invalidChar is not null)
                    context.AddFailure(
                        ValidationMessage.Text("Common.Validate.FolderName.InvalidChar", invalidChar));
            })
            .Custom((fileName, context) =>
            {
                var reservedWord = CheckReservedWord(fileName, []);
                if (reservedWord is not null)
                    context.AddFailure(
                        ValidationMessage.Text("Common.Validate.FolderName.ReservedWord", reservedWord));
            })
            .Must(x => !x.IsMatch(RegexPatterns.Ntfs83FileName))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderName.SpecialFormat"))
            .Must(x =>
            {
                if (ParentFolder is null) return true;

                var dirInfo = new DirectoryInfo(ParentFolder);
                if (!dirInfo.Exists) return true;
                if (IgnoreSameNameInParentFolder) return true;

                return !dirInfo
                    .EnumerateDirectories()
                    .Select(f => f.Name)
                    .Contains(
                        x,
                        IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            })
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderName.Duplicate"));
    }

}