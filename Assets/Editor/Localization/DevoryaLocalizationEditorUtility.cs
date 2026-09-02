using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

// 데보리아의 각 데이터 전용 Localization Editor가
// 공통으로 사용하는 Editor 전용 Utility.
//
// String Table Collection 생성,
// Locale별 String Table 생성,
// Entry 읽기/쓰기,
// LocalizedString 참조 생성,
// Localization Key용 문자열 변환 등을
// 한 곳에서 관리한다.
public static class DevoryaLocalizationEditorUtility
{
    // Localization String Table Collection의 기본 저장 위치.
    private const string DefaultTableDirectory =
        "Assets/Localization/StringTables";

    // 기존 String Table Collection을 이름으로 가져온다.
    public static StringTableCollection GetStringTableCollection(
        string tableCollectionName)
    {
        if (string.IsNullOrWhiteSpace(
                tableCollectionName))
        {
            return null;
        }

        return LocalizationEditorSettings
            .GetStringTableCollection(
                tableCollectionName
            );
    }

    // 지정된 String Table Collection을 가져오고,
    // 존재하지 않으면 자동으로 생성한다.
    public static StringTableCollection GetOrCreateStringTableCollection(
        string tableCollectionName)
    {
        if (string.IsNullOrWhiteSpace(
                tableCollectionName))
        {
            return null;
        }

        StringTableCollection collection =
            GetStringTableCollection(
                tableCollectionName
            );

        if (collection != null)
        {
            return collection;
        }

        EnsureFolderExists(
            DefaultTableDirectory
        );

        collection =
            LocalizationEditorSettings
                .CreateStringTableCollection(
                    tableCollectionName,
                    DefaultTableDirectory
                );

        return collection;
    }

    // 특정 Collection에 해당 Locale의 String Table이 존재하는지 확인하고,
    // 없으면 프로젝트에 등록된 Locale을 사용하여 새 Table을 생성한다.
    public static StringTable GetOrCreateStringTable(
        StringTableCollection collection,
        string localeCode)
    {
        if (collection == null ||
            string.IsNullOrWhiteSpace(
                localeCode))
        {
            return null;
        }

        StringTable existingTable =
            collection.GetTable(
                localeCode
            ) as StringTable;

        if (existingTable != null)
        {
            return existingTable;
        }

        Locale locale =
            LocalizationEditorSettings
                .GetLocale(
                    localeCode
                );

        if (locale == null)
        {
            return null;
        }

        return collection.AddNewTable(
            locale.Identifier
        ) as StringTable;
    }

    // String Table의 특정 Key 값을 가져온다.
    //
    // Entry가 존재하지 않으면 빈 문자열을 반환한다.
    public static string GetTableValue(
        StringTable table,
        string key)
    {
        if (table == null ||
            string.IsNullOrWhiteSpace(
                key))
        {
            return string.Empty;
        }

        StringTableEntry entry =
            table.GetEntry(
                key
            );

        return entry != null
            ? entry.Value
            : string.Empty;
    }

    // String Table에 Key가 없으면 새 Entry를 생성하고,
    // 이미 존재하면 기존 문자열을 갱신한다.
    public static void SetTableValue(
        StringTable table,
        string key,
        string value)
    {
        if (table == null ||
            string.IsNullOrWhiteSpace(
                key))
        {
            return;
        }

        StringTableEntry entry =
            table.GetEntry(
                key
            );

        if (entry == null)
        {
            table.AddEntry(
                key,
                value ?? string.Empty
            );
        }
        else
        {
            entry.Value =
                value ?? string.Empty;
        }

        EditorUtility.SetDirty(
            table
        );

        EditorUtility.SetDirty(
            table.SharedData
        );
    }

    // Collection과 Key를 사용하여
    // Table GUID + Entry ID 기반 LocalizedString 참조를 생성한다.
    //
    // 표시용 Key 이름이 이후 변경되더라도
    // 직접 문자열 이름으로 참조하는 방식보다 안전하게 유지할 수 있다.
    public static LocalizedString CreateLocalizedStringReference(
        StringTableCollection collection,
        string key)
    {
        if (collection == null ||
            collection.SharedData == null ||
            string.IsNullOrWhiteSpace(
                key))
        {
            return new LocalizedString();
        }

        SharedTableData.SharedTableEntry sharedEntry =
            collection.SharedData.GetEntry(
                key
            );

        if (sharedEntry == null)
        {
            return new LocalizedString();
        }

        return new LocalizedString(
            collection
                .SharedData
                .TableCollectionNameGuid,
            sharedEntry.Id
        );
    }

    // PascalCase / camelCase 문자열을
    // Localization Key에 사용하기 좋은 snake_case로 변환한다.
    //
    // 예:
    // ChangeSelectedPieceToJelluPawn
    // → change_selected_piece_to_jellu_pawn
    public static string ToSnakeCase(
        string source)
    {
        if (string.IsNullOrWhiteSpace(
                source))
        {
            return string.Empty;
        }

        return Regex.Replace(
                source,
                "([a-z0-9])([A-Z])",
                "$1_$2"
            )
            .ToLowerInvariant();
    }

    // Unity Localization Tables Editor 창을 연다.
    public static void OpenLocalizationTables()
    {
        EditorApplication.ExecuteMenuItem(
            "Window/Asset Management/Localization Tables"
        );
    }

    // Localization 저장 경로가 존재하지 않을 경우
    // 필요한 폴더를 순서대로 생성한다.
    private static void EnsureFolderExists(
        string folderPath)
    {
        if (AssetDatabase.IsValidFolder(
                folderPath))
        {
            return;
        }

        string[] parts =
            folderPath.Split('/');

        if (parts.Length == 0)
        {
            return;
        }

        string currentPath =
            parts[0];

        for (int i = 1;
             i < parts.Length;
             i++)
        {
            string nextPath =
                $"{currentPath}/{parts[i]}";

            if (AssetDatabase.IsValidFolder(
                    nextPath) == false)
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    parts[i]
                );
            }

            currentPath =
                nextPath;
        }
    }
}