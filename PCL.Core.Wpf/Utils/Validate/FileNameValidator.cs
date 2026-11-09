using System;
using System.IO;
using System.Linq;
using FluentValidation;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.Validate;

public class FileNameValidator : FileSystemValidator
{
    private bool? _isParentFolderExists;

    public FileNameValidator() : this(null)
    {
    }

    public bool UseMinecraftCharCheck { get; set; }
    public bool IgnoreCase { get; set; }
    public string? ParentFolder { get; set; }
    public bool RequireParentFolderExists { get; set; }

    public FileNameValidator(string? parentFolder = null,
        bool ignoreCase = true,
        bool useMinecraftCharCheck = true,
        bool requireParentFolderExists = true)
    {
        UseMinecraftCharCheck = useMinecraftCharCheck;
        IgnoreCase = ignoreCase;
        ParentFolder = parentFolder;
        RequireParentFolderExists = requireParentFolderExists;

        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.Empty"))
            .Must(x => !x.StartsWith(' '))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FileName.StartsWithSpace"))
            .Must(x => !x.EndsWith(' '))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FileName.EndsWithSpace"))
            .Must(x => !x.EndsWith('.'))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FileName.EndsWithDot"))
            .Custom((fileName, context) =>
            {
                var invalidChar = CheckInvalidStrings(
                    fileName,
                    UseMinecraftCharCheck ? ["!;"] : []);
                if (invalidChar is not null)
                    context.AddFailure(
                        ValidationMessage.Text("Common.Validate.FileName.InvalidChar", invalidChar));
            })
            .Custom((fileName, context) =>
            {
                var reservedWord = CheckReservedWord(fileName, []);
                if (reservedWord is not null)
                    context.AddFailure(
                        ValidationMessage.Text("Common.Validate.FileName.ReservedWord", reservedWord));
            })
            .Must(x => !x.IsMatch(RegexPatterns.Ntfs83FileName))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FileName.SpecialFormat"))
            .Must(x =>
            {
                _isParentFolderExists = null;
                if (ParentFolder is null) return true;

                var dirInfo = new DirectoryInfo(ParentFolder);
                if (dirInfo.Exists)
                    return !dirInfo
                        .EnumerateFiles()
                        .Select(f => f.Name)
                        .Contains(
                            x,
                            IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

                _isParentFolderExists = false;
                return !RequireParentFolderExists;
            })
            .WithMessage(_ => _isParentFolderExists is not null
                ? ValidationMessage.Text("Common.Validate.FileName.ParentFolderMissing", ParentFolder)
                : ValidationMessage.Text("Common.Validate.FileName.Duplicate"));
    }

}