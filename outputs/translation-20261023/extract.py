import pathlib,re,json
root=pathlib.Path(r"C:/Users/zntif/.codex/worktrees/status-effect-restrictions/Devorya")
out=pathlib.Path(r"C:/Users/zntif/OneDrive/Documents/GitHub/Devorya/outputs/translation-20261023")
def scalar(text):
 text=text.strip()
 if text.startswith('"'):
  text=re.sub(r"\\\r?\n\s*","",text)
  return json.loads(re.sub(r"\r?\n\s*"," ",text))
 if text.startswith("'") and text.endswith("'"):return re.sub(r"\r?\n\s*"," ",text[1:-1]).replace("''","'")
 return re.sub(r"\r?\n\s*"," ",text)
def read(p):
 t=p.read_text(encoding="utf-8-sig")
 obj={}
 g=re.search(r"^  m_TableCollectionNameGuidString: (.+)$",t,re.M)
 if g:obj["m_TableCollectionNameGuidString"]=g[1].strip()
 entries=[]
 for m in re.finditer(r"^  - m_Id: (\d+)\r?\n    (m_Key|m_Localized): ?(.*?)(?=\r?\n    m_Metadata:)",t,re.M|re.S):
  entries.append({"m_Id":m[1],m[2]:scalar(m[3])})
 if entries:obj["m_Entries" if "m_Key" in entries[0] else "m_TableData"]=entries
 else:
  links=[]
  for block in re.findall(r"^  localized[^:\n]*:.*?(?=^  \w|\Z)",t,re.M|re.S):
   table=re.search(r"m_TableCollectionName: *([^\r\n]*)",block)
   keyid=re.search(r"m_KeyId: *(\d+)",block)
   key=re.search(r"m_Key: *([^\r\n]*)",block)
   if table and keyid:links.append({"m_TableReference":{"m_TableCollectionName":table[1].strip()},"m_TableEntryReference":{"m_KeyId":keyid[1],"m_Key":key[1].strip() if key else ""}})
  obj["links"]=links
 return obj
def splitkey(key):
 for suffix in ["tooltip_description_format","condition_fail","description","name","format","tooltip"]:
  for sep in [".","_"]:
   if key.endswith(sep+suffix):return key[:-(len(suffix)+1)],suffix
 return key,"기타"
tables=[("특성","General_Skill"),("능력·스킬","Unique_Skill"),("아이템","Battle_Item"),("상태효과","Status_Effect"),("유물","Battle_Relic")]
result=[];guidmap={}
for label,table in tables:
 shared=read(root/"Assets/Localization/StringTables"/(table+" Shared Data.asset"))
 guidmap[shared["m_TableCollectionNameGuidString"]]=table
 langs={lang:{str(x["m_Id"]):x.get("m_Localized") or "" for x in read(root/"Assets/Localization/StringTables"/(table+"_"+lang+".asset")).get("m_TableData",[])} for lang in ["ko","en","ja"]}
 entries=shared.get("m_Entries",[])
 names={splitkey(e["m_Key"])[0]:langs["ko"].get(str(e["m_Id"]),"") for e in entries if splitkey(e["m_Key"])[1]=="name"}
 rows=[]
 for e in entries:
  key=e["m_Key"];id=str(e["m_Id"]);base,part=splitkey(key)
  vals=[langs[l].get(id,"") for l in ["ko","en","ja"]]
  missing=[l for l,v in zip(["한국어","영어","일본어"],vals) if not v.strip()]
  rows.append({"id":id,"name":names.get(base,base),"type":{"name":"이름","description":"설명","condition_fail":"사용 실패 안내","tooltip_description_format":"툴팁 설명","tooltip_format":"툴팁 설명","format":"툴팁 설명","tooltip":"툴팁 설명"}.get(part,part),"values":vals,"status":", ".join(missing)+" 누락" if missing else "입력됨","key":key,"assets":[]})
 result.append({"sheet":label,"table":table,"rows":rows})
lookup={(s["table"],r["id"]):r for s in result for r in s["rows"]}
def walk(v,asset):
 if isinstance(v,dict):
  if "m_TableReference" in v and "m_TableEntryReference" in v:
   table=v["m_TableReference"].get("m_TableCollectionName","")
   if table.startswith("GUID:"):table=guidmap.get(table[5:],"")
   entry=v["m_TableEntryReference"];row=lookup.get((table,str(entry.get("m_KeyId",0))))
   if row is None and entry.get("m_Key"):
    row=next((r for s in result if s["table"]==table for r in s["rows"] if r["key"]==entry["m_Key"]),None)
   if row and asset not in row["assets"]:row["assets"].append(asset)
  for x in v.values():walk(x,asset)
 elif isinstance(v,list):
  for x in v:walk(x,asset)
for folder in ["Skills","Item","StatusEffect","Relic"]:
 for p in (root/"Assets/Devorya/Data"/folder).rglob("*.asset"):
  walk(read(p),p.stem)
for s in result:
 named=[(splitkey(r["key"])[0],r["values"][0]) for r in s["rows"] if r["type"]=="이름"]
 for r in s["rows"]:
  if r["name"].startswith(("general_skill","unique_skill","battle_","status_")):
   candidates=[(k,n) for k,n in named if r["key"].startswith(k+".") or r["key"].startswith(k+"_")]
   if candidates:r["name"]=max(candidates,key=lambda x:len(x[0]))[1]
   elif r["assets"]:r["name"]=r["assets"][0].split("_",1)[-1]
for s in result:s["rows"].sort(key=lambda r:(r["name"],{"이름":0,"설명":1,"툴팁 설명":2}.get(r["type"],3),r["key"]))
(out/"translations.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps([{ "sheet":s["sheet"],"rows":len(s["rows"]),"missing":sum(r["status"]!="입력됨" for r in s["rows"])} for s in result],ensure_ascii=False))