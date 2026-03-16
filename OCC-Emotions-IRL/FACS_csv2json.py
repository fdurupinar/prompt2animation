import csv, sys, json

def openface_csv2json(emotion, duration, filename):
    #openface_aus = [1, 2, 4, 5, 6, 7, 9, 10, 12, 14, 15, 17, 20, 23, 25, 26, 28, 45]
    openface_aus = list(range(1,65)) # all AUs to support different sources
    facs = [] 
    with open(filename, newline='', encoding='utf-8') as csvfile:
        header = [h.strip() for h in csvfile.readline().split(',')]
        reader = csv.DictReader(csvfile, fieldnames=header)
        for value in reader:
            for i in openface_aus: 
                au_code = "AU%02d"%i
                #print(au_code)

                key = au_code + "_r"
                if key in value:
                    intensity = float(value[key].strip())
                    scaled_intensity = (intensity / 5.0) * 100
                    print(intensity, scaled_intensity)
                    au_animation = {}
                    au_animation["AU"] = i
                    au_animation["Times"] = [0, duration*0.1, duration]
                    au_animation["Intensities"] = [0, scaled_intensity, scaled_intensity]
                    facs.append(au_animation)

    animation = {}
    animation["duration"] = duration
    animation["emotion"] = emotion
    animation["facial_actions"] = facs
    savename = filename.replace(".csv", ".json")
    with open(savename, 'w', encoding='utf-8') as f:
        json.dump(animation, f, indent=4)

if __name__ == "__main__":
    if len(sys.argv) < 4:
        print(f"usage: {sys.argv[0]} emotion duration filename.csv")
        sys.exit(0)

    emotion = sys.argv[1]
    duration = float(sys.argv[2])
    filename = sys.argv[3]
    openface_csv2json(emotion, duration, filename)


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
