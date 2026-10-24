using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class PieceTypeStyleChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void ApplyAndRun()
    {
        try
        {
            var input=JsonUtility.FromJson<TooltipTranslationImport.Input>(File.ReadAllText("Logs/piece-type-annotations.json"));
            foreach(var row in input.entries)
            {
                var collection=LocalizationEditorSettings.GetStringTableCollection(row.collection);
                var table=collection.GetTable(new LocaleIdentifier(row.locale)) as StringTable;
                var entry=table.GetEntry(row.key);
                Check(entry!=null,"Existing entry "+row.key);
                var md=entry.GetMetadata<TooltipSemanticMetadata>();
                if(md==null){md=new TooltipSemanticMetadata();entry.AddMetadata(md);}
                foreach(var span in row.spans)
                {
                    Check(entry.Value.Contains(span.text),"Authored span still exists "+row.key);
                    if(!md.spans.Any(s=>s.text==span.text && s.role==TooltipTextRole.PieceType)) md.spans.Add(span);
                }
                EditorUtility.SetDirty(table);
            }
            var palette=AssetDatabase.LoadAssetAtPath<TooltipStylePalette>("Assets/Resources/TooltipStylePalette.asset");
            palette.pieceType=new Color32(183,166,217,255);
            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            Check(ColorUtility.ToHtmlStringRGB(palette.Get(TooltipTextRole.PieceType))=="B7A6D9","PieceType color");
            foreach(var row in input.entries)
            {
                var table=LocalizationEditorSettings.GetStringTableCollection(row.collection).GetTable(new LocaleIdentifier(row.locale)) as StringTable;
                var entry=table.GetEntry(row.key);
                string formatted=TooltipTextStyle.Format(entry.Value,entry.GetMetadata<TooltipSemanticMetadata>());
                foreach(var span in row.spans) Check(formatted.Contains("<color=#B7A6D9>"+span.text+"</color>"),"Localized PieceType "+row.locale+" "+row.key);
            }
            foreach(string token in new[]{"[폰]","[나이트]","[비숍]","[룩]","[퀸]","[킹]","[Pawn]","[Knight]","[Bishop]","[Rook]","[Queen]","[King]","［ポーン］","［ナイト］","［ビショップ］","［ルーク］","［クイーン］","［キング］"})
            {
                var md=new TooltipSemanticMetadata();
                md.spans.Add(new TooltipSemanticMetadata.Span{text=token,role=TooltipTextRole.PieceType});
                md.spans.Add(new TooltipSemanticMetadata.Span{text="Debuff",role=TooltipTextRole.Negative});
                md.spans.Add(new TooltipSemanticMetadata.Span{text="Buff",role=TooltipTextRole.Positive});
                md.spans.Add(new TooltipSemanticMetadata.Span{text="1 turn",role=TooltipTextRole.Value});
                var source="<size=80%><b>"+token+"</b></size> Debuff Buff 1 turn <color=#123456>plain</color>";
                var result=TooltipTextStyle.Format(source,md);
                Check(result.Contains("<size=80%><b><color=#B7A6D9>"+token+"</color></b></size>"),"Existing size/bold preserved");
                Check(result.Contains("<color=#D87969>Debuff</color>") && result.Contains("<color=#75C9D6>Buff</color>") && result.Contains("<color=#E4C668>1 turn</color>"),"Combined roles");
                Check(result.Contains("<color=#123456>plain</color>"),"Existing color preserved");
                Check(TooltipTextStyle.Format(source,(TooltipSemanticMetadata)null)==source,"No forced conversion");
            }
            Debug.Log("PIECE_TYPE_STYLE_PASS: 30 locale entries; 6 types x 3 languages; mixed roles, rich text and fallback");
            TooltipStyleChecks.Run();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
