import csv, sys, json, os
from FACS_csv2json import openface_csv2json

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(f"usage: {sys.argv[0]} duration directory")
        sys.exit(0)

    duration = float(sys.argv[1])
    directory = sys.argv[2]

    for toppath, topfolders, topfiles in os.walk(directory):
        for topfolder in topfolders:
            for path,folders,files in os.walk(os.path.join(directory, topfolder)):
                for file in files:
                    if ".csv" in file:
                        fullname = f"{directory}/{topfolder}/{file}"
                        with open(fullname) as f:
                            print(f"Reading '{fullname}'")
                            openface_csv2json(topfolder, duration, fullname)


"""
Example output format
{
    "duration": 4.5,
    "emotion": "Admiration",
    "facial_actions": [
        {
            "AU": 1,
            "Times": [
                0,
                2.5,
                4.5
            ],
            "Intensities": [
                0,
                60,
                60
            ]
        },
"""
