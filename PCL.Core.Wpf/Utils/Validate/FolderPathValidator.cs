using System;
using System.IO;
using FluentValidation;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.Validate;

public class FolderPathValidator : FileSystemValidator
{
    public FolderPathValidator() : this(true)
    {
    }

    public bool UseMinecraftCharCheck { get; set; }

    public FolderPathValidator(bool useMinecraftCharCheck)
    {
        UseMinecraftCharCheck = useMinecraftCharCheck;

        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.Empty"))
            .Must(x => !x.EndsWith(' '))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderName.EndsWithSpace"))
            .Must(x => !x.EndsWith('.'))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderName.EndsWithDot"));

        RuleForEach(x => _GetSubPaths(x))
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage(_ => ValidationMessage.Text("Common.Validate.FolderPath.Malformed"))
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
            .OverridePropertyName("PathSegments");
    }


    private static string[] _GetSubPaths(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return [];

        var fullPath = new DirectoryInfo(path).FullName;
        return fullPath[Path.GetPathRoot(fullPath)!.Length..]
            .TrimEnd(Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
    }
}