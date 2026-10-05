using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PieceDatabase))]
public class PieceDatabaseEditor : Editor
{
    private const string UncategorizedGroupName =
        "Uncategorized";

    private const string NullGroupName =
        "Missing / Null";


    private SerializedProperty pieceDataListProperty;

    private readonly Dictionary<string, bool>
        foldoutStates =
            new Dictionary<string, bool>();


    private string searchText =
        string.Empty;

    private PieceData pieceDataToAdd;

    private bool showRawArray;


    private sealed class PieceEntry
    {
        public int arrayIndex;

        public PieceData data;

        public string pieceId;

        public string pieceTypeName;

        public int pieceTypeOrder;

        public string speciesText;

        public string groupName;

        public bool isMissingId;

        public bool isDuplicateId;
    }


    private sealed class PieceGroup
    {
        public string name;

        public readonly List<PieceEntry> entries =
            new List<PieceEntry>();
    }


    private void OnEnable()
    {
        pieceDataListProperty =
            serializedObject.FindProperty(
                "pieceDataList"
            );
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        if (pieceDataListProperty == null)
        {
            EditorGUILayout.HelpBox(
                "PieceDatabase에서 pieceDataList를 찾지 못했습니다.",
                MessageType.Error
            );

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();

            return;
        }


        List<PieceEntry> allEntries =
            BuildEntries();


        MarkDuplicateIds(
            allEntries
        );


        DrawSearch();

        EditorGUILayout.Space(6f);


        DrawSummary(
            allEntries
        );

        EditorGUILayout.Space(6f);


        List<PieceGroup> groups =
            BuildGroups(
                allEntries
            );


        DrawFoldoutControls(
            groups
        );

        EditorGUILayout.Space(4f);


        int requestedRemoveIndex =
            DrawGroups(
                groups
            );


        if (requestedRemoveIndex >= 0)
        {
            RemoveElementAt(
                requestedRemoveIndex
            );
        }


        EditorGUILayout.Space(8f);

        DrawAddArea();


        EditorGUILayout.Space(8f);

        DrawRawArray();


        serializedObject.ApplyModifiedProperties();
    }


    // =====================================================
    // Search
    // =====================================================

    private void DrawSearch()
    {
        EditorGUILayout.LabelField(
            "Piece Database",
            EditorStyles.boldLabel
        );


        EditorGUILayout.BeginHorizontal();


        searchText =
            EditorGUILayout.TextField(
                "Search",
                searchText
            );


        if (GUILayout.Button(
                "Clear",
                GUILayout.Width(50f)
            ))
        {
            searchText =
                string.Empty;

            GUI.FocusControl(
                null
            );
        }


        EditorGUILayout.EndHorizontal();
    }


    // =====================================================
    // Summary
    // =====================================================

    private void DrawSummary(
        List<PieceEntry> entries)
    {
        int totalCount =
            entries.Count;

        int nullCount =
            0;

        int missingIdCount =
            0;


        HashSet<string>
            duplicateIds =
                new HashSet<string>(
                    StringComparer.Ordinal
                );


        for (int i = 0;
             i < entries.Count;
             i++)
        {
            PieceEntry entry =
                entries[i];


            if (entry.data == null)
            {
                nullCount++;
            }


            if (entry.isMissingId)
            {
                missingIdCount++;
            }


            if (entry.isDuplicateId &&
                string.IsNullOrEmpty(
                    entry.pieceId
                ) == false)
            {
                duplicateIds.Add(
                    entry.pieceId
                );
            }
        }


        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );


        EditorGUILayout.LabelField(
            $"Total Pieces : {totalCount}"
        );

        EditorGUILayout.LabelField(
            $"Null Entries : {nullCount}"
        );

        EditorGUILayout.LabelField(
            $"Missing Piece ID : {missingIdCount}"
        );

        EditorGUILayout.LabelField(
            $"Duplicate ID Groups : {duplicateIds.Count}"
        );


        EditorGUILayout.EndVertical();


        if (nullCount > 0)
        {
            EditorGUILayout.HelpBox(
                "Piece Data List에 null 항목이 있습니다.",
                MessageType.Warning
            );
        }


        if (missingIdCount > 0)
        {
            EditorGUILayout.HelpBox(
                "pieceId가 비어 있는 PieceData가 있습니다.",
                MessageType.Warning
            );
        }


        if (duplicateIds.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "동일한 pieceId를 사용하는 PieceData가 있습니다. " +
                "GetData(string pieceId) 결과가 모호해질 수 있으므로 확인해주세요.",
                MessageType.Error
            );
        }
    }


    // =====================================================
    // Foldout Controls
    // =====================================================

    private void DrawFoldoutControls(
        List<PieceGroup> groups)
    {
        EditorGUILayout.BeginHorizontal();


        if (GUILayout.Button(
                "Expand All"
            ))
        {
            for (int i = 0;
                 i < groups.Count;
                 i++)
            {
                foldoutStates[
                    groups[i].name
                ] = true;
            }
        }


        if (GUILayout.Button(
                "Collapse All"
            ))
        {
            for (int i = 0;
                 i < groups.Count;
                 i++)
            {
                foldoutStates[
                    groups[i].name
                ] = false;
            }
        }


        EditorGUILayout.EndHorizontal();
    }


    // =====================================================
    // Groups
    // =====================================================

    private int DrawGroups(
        List<PieceGroup> groups)
    {
        int requestedRemoveIndex =
            -1;


        if (groups.Count == 0)
        {
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(
                    searchText
                )
                    ? "등록된 PieceData가 없습니다."
                    : "검색 조건에 맞는 PieceData가 없습니다.",
                MessageType.Info
            );

            return requestedRemoveIndex;
        }


        for (int groupIndex = 0;
             groupIndex < groups.Count;
             groupIndex++)
        {
            PieceGroup group =
                groups[groupIndex];


            bool isExpanded =
                GetFoldoutState(
                    group.name
                );


            // Search 중에는 검색 결과가 바로 보이도록
            // 해당 그룹을 자동으로 펼친다.
            if (string.IsNullOrWhiteSpace(
                    searchText
                ) == false)
            {
                isExpanded =
                    true;
            }


            isExpanded =
                EditorGUILayout.Foldout(
                    isExpanded,
                    $"{group.name} ({group.entries.Count})",
                    true,
                    EditorStyles.foldoutHeader
                );


            foldoutStates[
                group.name
            ] = isExpanded;


            if (isExpanded == false)
            {
                continue;
            }


            EditorGUI.indentLevel++;


            for (int entryIndex = 0;
                 entryIndex < group.entries.Count;
                 entryIndex++)
            {
                PieceEntry entry =
                    group.entries[entryIndex];


                bool requestRemove =
                    DrawEntry(
                        entry
                    );


                if (requestRemove)
                {
                    requestedRemoveIndex =
                        entry.arrayIndex;
                }
            }


            EditorGUI.indentLevel--;


            EditorGUILayout.Space(3f);
        }


        return requestedRemoveIndex;
    }


    private bool DrawEntry(
        PieceEntry entry)
    {
        bool requestRemove =
            false;


        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );


        if (entry.data == null)
        {
            EditorGUILayout.LabelField(
                $"Array Index {entry.arrayIndex}",
                EditorStyles.boldLabel
            );


            EditorGUILayout.HelpBox(
                "Null PieceData",
                MessageType.Warning
            );
        }
        else
        {
            string displayId =
                string.IsNullOrEmpty(
                    entry.pieceId
                )
                    ? "(Missing ID)"
                    : entry.pieceId;


            EditorGUILayout.LabelField(
                displayId,
                EditorStyles.boldLabel
            );


            EditorGUILayout.LabelField(
                $"Type : {entry.pieceTypeName}"
            );


            EditorGUILayout.LabelField(
                $"Species : {entry.speciesText}"
            );


            EditorGUILayout.LabelField(
                $"Array Index : {entry.arrayIndex}"
            );


            if (entry.isMissingId)
            {
                EditorGUILayout.HelpBox(
                    "pieceId가 비어 있습니다.",
                    MessageType.Warning
                );
            }


            if (entry.isDuplicateId)
            {
                EditorGUILayout.HelpBox(
                    $"Duplicate pieceId : {entry.pieceId}",
                    MessageType.Error
                );
            }
        }


        EditorGUILayout.BeginHorizontal();


        SerializedProperty elementProperty =
            pieceDataListProperty
                .GetArrayElementAtIndex(
                    entry.arrayIndex
                );


        PieceData newData =
            (PieceData)
            EditorGUILayout.ObjectField(
                elementProperty.objectReferenceValue,
                typeof(PieceData),
                false
            );


        if (newData !=
            elementProperty.objectReferenceValue)
        {
            elementProperty.objectReferenceValue =
                newData;
        }


        using (
            new EditorGUI.DisabledScope(
                entry.data == null
            ))
        {
            if (GUILayout.Button(
                    "Select",
                    GUILayout.Width(55f)
                ))
            {
                Selection.activeObject =
                    entry.data;

                EditorGUIUtility.PingObject(
                    entry.data
                );
            }
        }


        if (GUILayout.Button(
                "X",
                GUILayout.Width(24f)
            ))
        {
            requestRemove =
                true;
        }


        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();


        return requestRemove;
    }


    // =====================================================
    // Add
    // =====================================================

    private void DrawAddArea()
    {
        EditorGUILayout.LabelField(
            "Add Piece Data",
            EditorStyles.boldLabel
        );


        EditorGUILayout.BeginHorizontal();


        pieceDataToAdd =
            (PieceData)
            EditorGUILayout.ObjectField(
                pieceDataToAdd,
                typeof(PieceData),
                false
            );


        using (
            new EditorGUI.DisabledScope(
                pieceDataToAdd == null
            ))
        {
            if (GUILayout.Button(
                    "Add",
                    GUILayout.Width(50f)
                ))
            {
                if (ContainsPieceData(
                        pieceDataToAdd
                    ))
                {
                    EditorUtility.DisplayDialog(
                        "Piece Database",
                        "이 PieceData는 이미 Piece Database에 등록되어 있습니다.",
                        "OK"
                    );
                }
                else
                {
                    int newIndex =
                        pieceDataListProperty
                            .arraySize;


                    pieceDataListProperty
                        .InsertArrayElementAtIndex(
                            newIndex
                        );


                    SerializedProperty
                        newElement =
                            pieceDataListProperty
                                .GetArrayElementAtIndex(
                                    newIndex
                                );


                    newElement.objectReferenceValue =
                        pieceDataToAdd;


                    pieceDataToAdd =
                        null;
                }
            }
        }


        EditorGUILayout.EndHorizontal();
    }


    private bool ContainsPieceData(
        PieceData targetData)
    {
        if (targetData == null)
        {
            return false;
        }


        for (int i = 0;
             i < pieceDataListProperty.arraySize;
             i++)
        {
            SerializedProperty element =
                pieceDataListProperty
                    .GetArrayElementAtIndex(
                        i
                    );


            if (element.objectReferenceValue ==
                targetData)
            {
                return true;
            }
        }


        return false;
    }


    // =====================================================
    // Raw Array
    // =====================================================

    private void DrawRawArray()
    {
        showRawArray =
            EditorGUILayout.Foldout(
                showRawArray,
                "Advanced / Raw Array",
                true
            );


        if (showRawArray == false)
        {
            return;
        }


        EditorGUILayout.HelpBox(
            "이 배열의 실제 순서는 Runtime에 그대로 유지됩니다. " +
            "GetData(PieceType)는 동일 타입이 여러 개일 경우 " +
            "배열에서 먼저 발견된 PieceData를 반환하므로 " +
            "순서를 임의로 변경할 때 주의해주세요.",
            MessageType.Warning
        );


        EditorGUILayout.PropertyField(
            pieceDataListProperty,
            true
        );
    }


    // =====================================================
    // Build Entries
    // =====================================================

    private List<PieceEntry> BuildEntries()
    {
        List<PieceEntry> entries =
            new List<PieceEntry>(
                pieceDataListProperty.arraySize
            );


        for (int i = 0;
             i < pieceDataListProperty.arraySize;
             i++)
        {
            SerializedProperty element =
                pieceDataListProperty
                    .GetArrayElementAtIndex(
                        i
                    );


            PieceData data =
                element.objectReferenceValue
                    as PieceData;


            PieceEntry entry =
                BuildEntry(
                    i,
                    data
                );


            entries.Add(
                entry
            );
        }


        return entries;
    }


    private PieceEntry BuildEntry(
        int arrayIndex,
        PieceData data)
    {
        PieceEntry entry =
            new PieceEntry();


        entry.arrayIndex =
            arrayIndex;

        entry.data =
            data;


        if (data == null)
        {
            entry.pieceId =
                string.Empty;

            entry.pieceTypeName =
                "-";

            entry.pieceTypeOrder =
                int.MaxValue;

            entry.speciesText =
                "-";

            entry.groupName =
                NullGroupName;

            entry.isMissingId =
                false;

            return entry;
        }


        SerializedObject dataObject =
            new SerializedObject(
                data
            );


        SerializedProperty pieceIdProperty =
            dataObject.FindProperty(
                "pieceId"
            );

        SerializedProperty pieceTypeProperty =
            dataObject.FindProperty(
                "pieceType"
            );

        SerializedProperty speciesTagsProperty =
            dataObject.FindProperty(
                "speciesTags"
            );


        entry.pieceId =
            pieceIdProperty != null
                ? pieceIdProperty.stringValue
                : data.name;


        entry.isMissingId =
            string.IsNullOrWhiteSpace(
                entry.pieceId
            );


        if (pieceTypeProperty != null &&
            pieceTypeProperty.propertyType ==
            SerializedPropertyType.Enum)
        {
            entry.pieceTypeOrder =
                pieceTypeProperty.enumValueIndex;

            entry.pieceTypeName =
                GetEnumDisplayName(
                    pieceTypeProperty
                );
        }
        else
        {
            entry.pieceTypeOrder =
                int.MaxValue;

            entry.pieceTypeName =
                "-";
        }


        GetSpeciesInfo(
            speciesTagsProperty,
            out string groupName,
            out string speciesText
        );


        entry.groupName =
            groupName;

        entry.speciesText =
            speciesText;


        return entry;
    }


    // =====================================================
    // Species Group
    // =====================================================

    private void GetSpeciesInfo(
        SerializedProperty speciesTagsProperty,
        out string groupName,
        out string speciesText)
    {
        groupName =
            UncategorizedGroupName;

        speciesText =
            "None";


        if (speciesTagsProperty == null ||
            speciesTagsProperty.isArray == false ||
            speciesTagsProperty.arraySize == 0)
        {
            return;
        }


        List<string> speciesNames =
            new List<string>();


        for (int i = 0;
             i < speciesTagsProperty.arraySize;
             i++)
        {
            SerializedProperty tagProperty =
                speciesTagsProperty
                    .GetArrayElementAtIndex(
                        i
                    );


            if (tagProperty.propertyType !=
                SerializedPropertyType.Enum)
            {
                continue;
            }


            string tagName =
                GetEnumDisplayName(
                    tagProperty
                );


            if (string.IsNullOrEmpty(
                    tagName
                ))
            {
                continue;
            }


            if (string.Equals(
                    tagName,
                    "None",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                continue;
            }


            speciesNames.Add(
                tagName
            );


            // Folder group uses the first valid Species Tag.
            if (groupName ==
                UncategorizedGroupName)
            {
                groupName =
                    tagName;
            }
        }


        if (speciesNames.Count > 0)
        {
            speciesText =
                string.Join(
                    ", ",
                    speciesNames
                );
        }
    }


    private string GetEnumDisplayName(
        SerializedProperty enumProperty)
    {
        if (enumProperty == null ||
            enumProperty.propertyType !=
            SerializedPropertyType.Enum)
        {
            return string.Empty;
        }


        int enumIndex =
            enumProperty.enumValueIndex;


        string[] displayNames =
            enumProperty.enumDisplayNames;


        if (displayNames != null &&
            enumIndex >= 0 &&
            enumIndex < displayNames.Length)
        {
            return displayNames[
                enumIndex
            ];
        }


        string[] enumNames =
            enumProperty.enumNames;


        if (enumNames != null &&
            enumIndex >= 0 &&
            enumIndex < enumNames.Length)
        {
            return enumNames[
                enumIndex
            ];
        }


        return string.Empty;
    }


    // =====================================================
    // Duplicate Check
    // =====================================================

    private void MarkDuplicateIds(
        List<PieceEntry> entries)
    {
        Dictionary<string, List<PieceEntry>>
            entriesById =
                new Dictionary<string, List<PieceEntry>>(
                    StringComparer.Ordinal
                );


        for (int i = 0;
             i < entries.Count;
             i++)
        {
            PieceEntry entry =
                entries[i];


            entry.isDuplicateId =
                false;


            if (entry.data == null ||
                entry.isMissingId)
            {
                continue;
            }


            if (entriesById.TryGetValue(
                    entry.pieceId,
                    out List<PieceEntry> sameIdEntries
                ) == false)
            {
                sameIdEntries =
                    new List<PieceEntry>();


                entriesById.Add(
                    entry.pieceId,
                    sameIdEntries
                );
            }


            sameIdEntries.Add(
                entry
            );
        }


        foreach (
            KeyValuePair<string, List<PieceEntry>>
                pair
            in entriesById)
        {
            if (pair.Value.Count <= 1)
            {
                continue;
            }


            for (int i = 0;
                 i < pair.Value.Count;
                 i++)
            {
                pair.Value[i]
                    .isDuplicateId =
                        true;
            }
        }
    }


    // =====================================================
    // Group Build / Search / Sort
    // =====================================================

    private List<PieceGroup> BuildGroups(
        List<PieceEntry> entries)
    {
        Dictionary<string, PieceGroup>
            groupMap =
                new Dictionary<string, PieceGroup>(
                    StringComparer.OrdinalIgnoreCase
                );


        for (int i = 0;
             i < entries.Count;
             i++)
        {
            PieceEntry entry =
                entries[i];


            if (MatchesSearch(
                    entry
                ) == false)
            {
                continue;
            }


            if (groupMap.TryGetValue(
                    entry.groupName,
                    out PieceGroup group
                ) == false)
            {
                group =
                    new PieceGroup();

                group.name =
                    entry.groupName;


                groupMap.Add(
                    entry.groupName,
                    group
                );
            }


            group.entries.Add(
                entry
            );
        }


        List<PieceGroup> groups =
            new List<PieceGroup>(
                groupMap.Values
            );


        groups.Sort(
            CompareGroups
        );


        for (int i = 0;
             i < groups.Count;
             i++)
        {
            groups[i].entries.Sort(
                CompareEntries
            );
        }


        return groups;
    }


    private bool MatchesSearch(
        PieceEntry entry)
    {
        if (string.IsNullOrWhiteSpace(
                searchText
            ))
        {
            return true;
        }


        string search =
            searchText.Trim();


        if (entry.data == null)
        {
            return "null".IndexOf(
                       search,
                       StringComparison.OrdinalIgnoreCase
                   ) >= 0 ||
                   NullGroupName.IndexOf(
                       search,
                       StringComparison.OrdinalIgnoreCase
                   ) >= 0;
        }


        return ContainsIgnoreCase(
                   entry.data.name,
                   search
               ) ||
               ContainsIgnoreCase(
                   entry.pieceId,
                   search
               ) ||
               ContainsIgnoreCase(
                   entry.pieceTypeName,
                   search
               ) ||
               ContainsIgnoreCase(
                   entry.speciesText,
                   search
               ) ||
               ContainsIgnoreCase(
                   entry.groupName,
                   search
               );
    }


    private bool ContainsIgnoreCase(
        string source,
        string search)
    {
        if (string.IsNullOrEmpty(
                source
            ))
        {
            return false;
        }


        return source.IndexOf(
                   search,
                   StringComparison.OrdinalIgnoreCase
               ) >= 0;
    }


    private int CompareGroups(
        PieceGroup a,
        PieceGroup b)
    {
        if (a.name ==
            NullGroupName)
        {
            return 1;
        }


        if (b.name ==
            NullGroupName)
        {
            return -1;
        }


        if (a.name ==
            UncategorizedGroupName)
        {
            return 1;
        }


        if (b.name ==
            UncategorizedGroupName)
        {
            return -1;
        }


        return StringComparer
            .OrdinalIgnoreCase
            .Compare(
                a.name,
                b.name
            );
    }


    private int CompareEntries(
        PieceEntry a,
        PieceEntry b)
    {
        int typeCompare =
            a.pieceTypeOrder.CompareTo(
                b.pieceTypeOrder
            );


        if (typeCompare != 0)
        {
            return typeCompare;
        }


        string aName =
            string.IsNullOrEmpty(
                a.pieceId
            )
                ? a.data != null
                    ? a.data.name
                    : string.Empty
                : a.pieceId;


        string bName =
            string.IsNullOrEmpty(
                b.pieceId
            )
                ? b.data != null
                    ? b.data.name
                    : string.Empty
                : b.pieceId;


        return StringComparer
            .OrdinalIgnoreCase
            .Compare(
                aName,
                bName
            );
    }


    // =====================================================
    // Array
    // =====================================================

    private void RemoveElementAt(
        int arrayIndex)
    {
        if (arrayIndex < 0 ||
            arrayIndex >=
            pieceDataListProperty.arraySize)
        {
            return;
        }


        SerializedProperty element =
            pieceDataListProperty
                .GetArrayElementAtIndex(
                    arrayIndex
                );


        // ObjectReference arrays can retain a null slot
        // depending on deletion behaviour, so clear it first.
        element.objectReferenceValue =
            null;


        pieceDataListProperty
            .DeleteArrayElementAtIndex(
                arrayIndex
            );
    }


    private bool GetFoldoutState(
        string groupName)
    {
        if (foldoutStates.TryGetValue(
                groupName,
                out bool isExpanded
            ))
        {
            return isExpanded;
        }


        // Groups are open by default the first time.
        foldoutStates[
            groupName
        ] = true;


        return true;
    }
}
