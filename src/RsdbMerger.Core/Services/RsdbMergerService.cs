using RsdbMerger.Core.Mergers;

namespace RsdbMerger.Core.Services;

public class RsdbMergerService
{
    public static IRsdbMerger GetMerger(ReadOnlySpan<char> canonical)
    {
        return canonical switch {
            "RSDB/GameSafetySetting.Product.rstbl.byml" => RsdbUniqueRowMergers.NameHash,
            "RSDB/RumbleCall.Product.rstbl.byml" or
            "RSDB/UIScreen.Product.rstbl.byml" => RsdbUniqueRowMergers.Name,
            "RSDB/TagDef.Product.rstbl.byml" => RsdbUniqueRowMergers.FullTagId,
            "RSDB/ActorInfo.Product.rstbl.byml" or
            "RSDB/AttachmentActorInfo.Product.rstbl.byml" or
            "RSDB/Challenge.Product.rstbl.byml" or
            "RSDB/EnhancementMaterialInfo.Product.rstbl.byml" or
            "RSDB/EventPlayEnvSetting.Product.rstbl.byml" or
            "RSDB/EventSetting.Product.rstbl.byml" or
            "RSDB/GameActorInfo.Product.rstbl.byml" or
            "RSDB/GameAnalyzedEventInfo.Product.rstbl.byml" or
            "RSDB/GameEventBaseSetting.Product.rstbl.byml" or
            "RSDB/GameEventMetadata.Product.rstbl.byml" or
            "RSDB/LoadingTips.Product.rstbl.byml" or
            "RSDB/Location.Product.rstbl.byml" or
            "RSDB/LocatorData.Product.rstbl.byml" or
            "RSDB/PouchActorInfo.Product.rstbl.byml" or
            "RSDB/XLinkPropertyTable.Product.rstbl.byml" or
            "RSDB/XLinkPropertyTableList.Product.rstbl.byml" => RsdbUniqueRowMergers.RowId,
            "RSDB/Tag.Product.rstbl.byml" => RsdbTagMerger.Shared,
            _ => throw new ArgumentException($"""
                No merge is registered for the RSDB file '{canonical}'
                """)
        };
    }
}
