using System.Globalization;

namespace FfxiTempLogCollector.Core;

public sealed class RawRecordFactory
{
    public RawRecord Create(
        RawRecordContext context,
        TempLogRawRecord parsedRecord,
        DecodedLogMessage decodedMessage,
        ExtractedTimestamp? timestamp = null,
        DetectedMarker? marker = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(parsedRecord);
        ArgumentNullException.ThrowIfNull(decodedMessage);

        ArgumentException.ThrowIfNullOrWhiteSpace(context.SessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.SourceFile);

        var rawRecordIdPrefix = string.Concat(
            context.SourceFile,
            parsedRecord.RecordIndex.ToString(CultureInfo.InvariantCulture));

        return new RawRecord
        {
            RawRecordId = HashUtil.ComputeSha1(
                rawRecordIdPrefix,
                parsedRecord.RawRecordBytes),
            SessionId = context.SessionId,
            FirstSeenAt = context.FirstSeenAt,
            SourceFile = context.SourceFile,
            WindowId = context.WindowId,
            RotationIndex = context.RotationIndex,
            FileMtime = context.FileMtime,
            FileSize = context.FileSize,
            FileHash = context.FileHash,
            RecordIndex = parsedRecord.RecordIndex,
            RecordOffset = parsedRecord.RecordOffset,
            RawRecordHash = HashUtil.ComputeSha1(parsedRecord.RawRecordBytes),
            MetaFields = [.. parsedRecord.MetaFields.Fields],
            EventGroup = parsedRecord.MetaFields.EventGroup,
            SequenceHint = parsedRecord.MetaFields.SequenceHint,
            TemplateHint = parsedRecord.MetaFields.TemplateHint,
            RawMessageHex = decodedMessage.RawMessageHex,
            VisibleText = decodedMessage.VisibleText,
            MessageTimeText = timestamp?.TimeText,
            MessageTimePrecision = timestamp?.Precision,
            IsMarker = marker?.IsMarker ?? false,
            MarkerKeyword = marker?.Keyword,
            ParseStatus = parsedRecord.ParseStatus,
            ParseError = parsedRecord.ParseError,
        };
    }
}
