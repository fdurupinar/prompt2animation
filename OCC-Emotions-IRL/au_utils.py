import json, os
import numpy as np

def read_au_names(filename):
    contents = [''] * 65 
    with open("au_names.txt", "r") as f:
        lines = f.readlines()
        for line in lines:
            tokens = line.split(".")
            num = int(tokens[0])
            name = tokens[1].strip()

            contents[num] = name
    return contents

def read_aus(topfolder):
    aus = np.zeros((110,65))
    names = []
    i = 0
    for path,folders,files in os.walk(topfolder):
        for file in files:
            if ".json" in file and ".meta" not in file:
                jsonname = f"{topfolder}/{file}"
                names.append(file[:-5])
                with open(jsonname, 'r', encoding='utf-8') as f:
                    print(f"Reading '{jsonname}'")
                    loaded_data = json.load(f)
                    #print(loaded_data)
                    for action in loaded_data["facial_actions"]:
                        au_id = action["AU"]
                        times = action["Times"]
                        intensities = action["Intensities"]
                        intensity = max(intensities)
                        #print(au_id, intensity)
                        aus[i][au_id] = intensity
                    i = i + 1
    aus = np.transpose(aus)
    return names, aus

def write_aus_csv(csvfilename, aus):
    with open(csvfilename, "w") as csvf:
        header = ""
        data = ""
        comma = ""
        for k in range(1, 65):
            if aus[k] > 0:
                auid = "%02d"%k
                header = header + comma + f"AU{auid}_r"
                intensity = 5*(aus[k]/100.0) 
                #print("  ", aus[k], intensity)
                data = data + comma + "%f"%(intensity)
                comma = ","
        csvf.writelines(header+"\n")
        csvf.writelines(data+"\n")

