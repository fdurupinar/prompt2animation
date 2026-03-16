# Prompt-to-Animation: Generating Cognitively-Grounded Facial Expressions with LLMs

> Unity 6000.2.2f1 • High Definition Render Pipeline (HDRP)

This Unity project provides a system for playing facial animations on 3D characters using the **Facial Action Coding System (FACS)**. Animations are defined in JSON files and generated with LLMs.

---

## &#x20;Requirements

- **Engine:** Unity **6000.2.2f1**
- **Render Pipeline:** High Definition Render Pipeline (HDRP)
- **Core Packages:** Input System, TextMesh Pro



---

## Requirements

- Unity **6000.2.2f1** (latest tested version; project may also run in earlier Unity 6 releases)
- Modern GPU supporting DX11+/DX12, Metal, or Vulkan (no specialized GPU required; 4GB+ VRAM recommended for HDRP)

---

## Getting Started

1. **Clone** the repo:
   ```bash
   git clone {{REPO_URL}} && cd {{REPO_DIR}}
   ```
2. Open the project in **Unity Hub**, ensure editor **6000.2.2f1** is selected.
3. On first launch, Unity will **resolve packages**.
4. Open **Window ▸ Rendering ▸ HDRP Wizard** and click **Fix All** (render pipeline, quality, and platform checks).
5. Open **Assets/Scenes/Main.unity** and press **Play**.

---

## Playing Animations

- In the scene **Main.unity**, select the **Characters** GameObject.
- Drag and drop a **JSON file** into the **Scenario** field.

---

## 📄 JSON Format

```json
{
  "duration": <float>,
  "emotion": "<emotion_name>",
  "facial_actions": [
    {
      "AU": <int>,
      "Times": [array of floats],
      "Intensities": [array of integers]
    }
  ]
}
```

- **duration**: total animation length in seconds.
- **emotion**: name of the expressed emotion.
- **facial\_actions**: list of Action Units (AUs) with time–intensity keyframes.
  - **AU**: FACS Action Unit number.
  - **Times**: ordered list of keyframe times (floats) relative to the start of the animation (always beginning at `0.0`).
  - **Intensities**: values in the range `[0, 100]`, corresponding one-to-one with each time in `Times`. Represents AU activation strength at that moment.
  - Example: `Times: [0.0, 1.2, 4.4, 5.6]`, `Intensities: [0, 35, 78, 100]` means the AU starts inactive, gradually activates, and peaks at intensity 100 by the end of the 5.6s mark.

---

## 🧠 LLM Prompt Template

Use the following prompt when generating animation JSONs with an LLM:

```text
Task: You are a FACS expert. Generate a facial expression
animation JSON file from the following description, considering
Ortony, Collins, Clore (OCC) emotions.
Instructions: Output a single JSON object with the following
fields:
1) "duration": estimated animation duration in seconds
2) "emotion": name of the OCC emotion
3) "facial_actions": list of facial Action Units and AUs 51 to
64 for head and eye movement. Each AU should be a dictionary
with:
- "AU": the AU number (e.g., 1, 12, 17, 51, etc.)
- "Times": list of key time points (starting at 0, ending
at duration)
- "Intensities": list of values (0–100) matching Times
Requirements:
- Match emotional expressivity and timing to the described
scenario.
- Do not include any other text outside the JSON object.
- Use valid JSON syntax with double quotes, arrays, and no
trailing commas.
- Ensure all lists are the same length and well-formed.
```

> Tip: Provide the LLM with a concise scenario (1–3 sentences) describing the emotion trigger, intensity, and timing cues.

---

---

## 🧭 Scenes&#x20;

- **Main.unity**: Contains character prefabs and controllers for animation playback.
- &#x20;**ControlPanel.unity**: Similar to Main.unity with  AUs superimposed on the face

---

## 🧩 Naming Conventions

- JSON files: `<emotion_name><scenario_id>.json`
- Example: `Joy1.json`, `Anger3.json`

---

## 🧪 Testing

- Drop test JSONs from the folders under`Resources` into the scenario field to validate playback. For example,  `Resources/OCC-Gemini/` has the animations for all the scenarios in the paper.

---

## 📜 License

GNU General Public License v3.0 

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

---

## 📚 Cite as

Funda Durupinar, Aline Normoyle, *Prompt-to-Animation: Generating Cognitively-Grounded Facial Expressions with LLMs*, Proceedings of the 18th ACM SIGGRAPH Conference on Motion, Interaction, and Games, 2025

### BibTeX

```bibtex
@inproceedings{Durupinar2025PromptToAnimation,
  author    = {Funda Durupinar and Aline Normoyle},
  title     = {Prompt-to-Animation: Generating Cognitively-Grounded Facial Expressions with LLMs},
  booktitle = {Proceedings of the 18th ACM SIGGRAPH Conference on Motion, Interaction, and Games},
  year      = {2025}
}
```

---

## 🧾 LLM Prompt Template

```
Task: You are a FACS expert. Generate a facial expression
animation JSON file from the following description, considering
Ortony, Collins, Clore (OCC) emotions.

Instructions: Output a single JSON object with the following
fields:
1) "duration": estimated animation duration in seconds
2) "emotion": name of the OCC emotion
3) "facial_actions": list of facial Action Units and AUs 51 to
   64 for head and eye movement. Each AU should be a dictionary
   with:
   - "AU": the AU number (e.g., 1, 12, 17, 51, etc.)
   - "Times": list of key time points (starting at 0, ending
     at duration)
   - "Intensities": list of values (0–100) matching Times

Requirements:
- Match emotional expressivity and timing to the described
  scenario.
- Do not include any other text outside the JSON object.
- Use valid JSON syntax with double quotes, arrays, and no
  trailing commas.
- Ensure all lists are the same length and well-formed.
```

