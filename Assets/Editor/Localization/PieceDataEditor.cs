using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// <변경부분>
// PieceData의 플레이어 표시용 이름을
// Data-owned Localization으로 관리하기 위한 전용 Editor.
//
// pieceId는 PieceDatabase에서 사용하는 고유 내부 ID이므로
// Localization Key의 stable identity로 재사용한다.
//
// 표시 이름 자체가 바뀌더라도 pieceId가 유지되는 한
// 기존 Localization Key와 번역 데이터는 유지된다.
[CustomEditor(typeof(PieceData))]
public class PieceDataEditor : Editor
{
    private const string TableCollectionName =
        "Piece_Data";

    private bool showTranslations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 기존 PieceData 필드는 모두 그대로 유지한다.
        //
        // LocalizedString 참조만 아래 Localization 관리 영역에서
        // 자동 연결하므로 기본 Inspector에서는 숨긴다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedDisplayName"
        );

        serializedObject.ApplyModifiedProperties();

        PieceData pieceData =
            (PieceData)target;

        EditorGUILayout.Space(10);

        DrawLocalizationInspector(
            pieceData
        );
    }

    private void DrawLocalizationInspector(
        PieceData pieceData)
    {
        EditorGUILayout.LabelField(
            "Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            if (string.IsNullOrWhiteSpace(
                    pieceData.pieceId))
            {
                EditorGUILayout.HelpBox(
                    "Localization Key를 생성하려면 " +
                    "먼저 Piece ID를 지정해주세요.",
                    MessageType.Warning
                );

                return;
            }

            string baseKey =
                GetPieceBaseKey(
                    pieceData
                );

            string nameKey =
                $"{baseKey}.name";

            EditorGUILayout.LabelField(
                "자동 Localization Key",
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                "Name",
                nameKey
            );

            EditorGUILayout.Space(6);

            if (string.IsNullOrWhiteSpace(
                    pieceData.displayName))
            {
                EditorGUILayout.HelpBox(
                    "Display Name이 비어 있습니다.\n" +
                    "플레이어에게 표시할 한국어 기물 이름을 먼저 입력해주세요.",
                    MessageType.Warning
                );
            }

            // 빈 Display Name이 실수로 KO Table에 들어가지 않도록
            // 한국어 원문을 작성한 뒤에만 동기화할 수 있게 한다.
            using (new EditorGUI.DisabledScope(
                       string.IsNullOrWhiteSpace(
                           pieceData.displayName)))
            {
                if (GUILayout.Button(
                        "Localization 생성 / 한국어 동기화"))
                {
                    SyncLocalization(
                        pieceData,
                        nameKey
                    );
                }
            }

            EditorGUILayout.Space(4);

            bool localizationReady =
                pieceData.localizedDisplayName != null &&
                pieceData.localizedDisplayName.IsEmpty == false;

            if (localizationReady == false)
            {
                EditorGUILayout.HelpBox(
                    "아직 Localization이 연결되지 않았습니다.\n" +
                    "Display Name 입력 후 위 버튼을 누르면 " +
                    "Piece_Data Table Key 생성, 한국어 원문 등록, " +
                    "LocalizedString 연결을 한 번에 처리합니다.",
                    MessageType.Info
                );

                DrawOpenLocalizationTableButton();

                return;
            }

            EditorGUILayout.HelpBox(
                "Localization 연결 완료\n" +
                "Display Name을 수정한 뒤 " +
                "\"Localization 생성 / 한국어 동기화\"를 다시 누르면 " +
                "한국어 값만 갱신됩니다. English / Japanese 번역은 유지됩니다.",
                MessageType.Info
            );

            EditorGUILayout.Space(4);

            showTranslations =
                EditorGUILayout.Foldout(
                    showTranslations,
                    "Translations",
                    true
                );

            if (showTranslations)
            {
                DrawTranslations(
                    nameKey
                );
            }

            EditorGUILayout.Space(6);

            DrawOpenLocalizationTableButton();
        }
    }

    private void DrawTranslations(
        string nameKey)
    {
        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            EditorGUILayout.HelpBox(
                $"{TableCollectionName} String Table Collection을 찾을 수 없습니다.",
                MessageType.Warning
            );

            return;
        }

        DrawLocaleTranslation(
            collection,
            "English",
            "en",
            nameKey
        );

        EditorGUILayout.Space(8);

        DrawLocaleTranslation(
            collection,
            "Japanese",
            "ja",
            nameKey
        );
    }

    private void DrawLocaleTranslation(
        StringTableCollection collection,
        string displayName,
        string localeCode,
        string nameKey)
    {
        StringTable table =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    localeCode
                );

        EditorGUILayout.LabelField(
            displayName,
            EditorStyles.boldLabel
        );

        if (table == null)
        {
            EditorGUILayout.HelpBox(
                $"{displayName} Locale 또는 String Table을 찾을 수 없습니다.",
                MessageType.Warning
            );

            return;
        }

        string currentName =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    nameKey
                );

        EditorGUI.BeginChangeCheck();

        string newName =
            EditorGUILayout.TextField(
                "Name",
                currentName
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                table,
                $"Edit {displayName} Piece Localization"
            );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    nameKey,
                    newName
                );

            EditorUtility.SetDirty(
                collection.SharedData
            );
        }
    }

    // 기존 한국어 Display Name을 KO Table에 동기화하고
    // LocalizedString 참조를 자동 연결한다.
    //
    // English / Japanese 값은 건드리지 않는다.
    private void SyncLocalization(
        PieceData pieceData,
        string nameKey)
    {
        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            EditorUtility.DisplayDialog(
                "Localization 생성 실패",
                "Piece_Data String Table Collection을 " +
                "생성하거나 찾을 수 없습니다.",
                "확인"
            );

            return;
        }

        StringTable koreanTable =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    "ko"
                );

        if (koreanTable == null)
        {
            EditorUtility.DisplayDialog(
                "Localization 생성 실패",
                "Korean (ko) Locale 또는 String Table을 " +
                "찾을 수 없습니다.",
                "확인"
            );

            return;
        }

        Undo.RecordObject(
            pieceData,
            "Sync Piece Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Piece Korean Localization"
        );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                nameKey,
                pieceData.displayName
            );

        pieceData.localizedDisplayName =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    nameKey
                );

        if (pieceData.localizedDisplayName == null ||
            pieceData.localizedDisplayName.IsEmpty)
        {
            EditorUtility.DisplayDialog(
                "Localization 연결 실패",
                "Localization Key 생성 결과를 확인해주세요.",
                "확인"
            );

            return;
        }

        EditorUtility.SetDirty(
            pieceData
        );

        EditorUtility.SetDirty(
            koreanTable
        );

        EditorUtility.SetDirty(
            collection.SharedData
        );

        AssetDatabase.SaveAssets();

        serializedObject.Update();

        Debug.Log(
            $"Piece Localization 동기화 완료: " +
            $"{pieceData.name} / {nameKey}"
        );
    }

    // PieceData의 고유 내부 ID를 Localization identity로 사용한다.
    //
    // JelluRook
    // -> piece.jellu_rook
    private string GetPieceBaseKey(
        PieceData pieceData)
    {
        string snakeCaseId =
            DevoryaLocalizationEditorUtility
                .ToSnakeCase(
                    pieceData.pieceId
                );

        return
            $"piece.{snakeCaseId}";
    }

    private void DrawOpenLocalizationTableButton()
    {
        if (GUILayout.Button(
                "Localization Tables 열기"))
        {
            DevoryaLocalizationEditorUtility
                .OpenLocalizationTables();
        }
    }
}