# Viseme-AU Confounding Analysis for CC4

## Overview
This document identifies confounding situations where CC4 visemes and Action Units (AUs) create conflicting or ambiguous mouth movements. The system has two independent animation systems that can interact negatively:
- **Visemes** (lip-sync from speech)
- **Action Units** (FACS-based emotional expressions)

Both systems drive the same blendshapes and jaw rotation, creating potential conflicts.

---

## System Architecture

### Visemes in CC4
**OVRLipSync Viseme Mapping** (15 visemes):
1. `sil` - Silence
2. `PP` → `B_M_P` - Bilabials (B, P, M)
3. `FF` → `F_V` - Labiodental (F, V)
4. `TH` → `TH` - Dental (TH)
5. `DD` → `T_L_D_N` - Alveolar (D, T, L, N)
6. `kk` → `K_G_H_NG` - Velar (K, G, NG)
7. `CH` → `CH_J` - Affricate (CH, J)
8. `SS` → `S_Z` - Fricative (S, Z)
9. `nn` → `T_L_D_N` - Alveolar nasal (N) **[DUPLICATE]**
10. `RR` → `R` - Rhotic (R)
11. `aa` → `AH` - Open vowel (A)
12. `E` → `EE` - Close vowel (E)
13. `ih` → `IH` - Near-close vowel (I)
14. `oh` → `OH` - Close-mid vowel (O)
15. `ou` → `W_OO` - Close back vowel (OO, W)

### CC4 Mouth Blendshapes (Relevant to both Visemes & AUs)
- `B_M_P`, `F_V`, `TH`, `T_L_D_N`, `K_G_H_NG`, `CH_J`, `S_Z`, `R`
- `AH`, `EE`, `IH`, `OH`, `W_OO`, `AE`
- `MOUTH_SHRUG_UPPER`, `MOUTH_SHRUG_LOWER`
- `MOUTH_PUCKER_UP_L`, `MOUTH_PUCKER_UP_R`
- `MOUTH_FUNNEL_UP_L`, `MOUTH_FUNNEL_UP_R`
- `MOUTH_FUNNEL_DOWN_L`, `MOUTH_FUNNEL_DOWN_R`
- `MOUTH_DOWN_LOWER_L`, `MOUTH_DOWN_LOWER_R`
- `MOUTH_FROWN_L`, `MOUTH_FROWN_R`
- `MOUTH_TIGHTEN_L`, `MOUTH_TIGHTEN_R`
- `MOUTH_STRETCH_L`, `MOUTH_STRETCH_R`
- `JAW_OPEN`

### Action Units Affecting Mouth
- **AU10** (Mouth Shrug Upper) - `MOUTH_SHRUG_UPPER`
- **AU12** (Mouth Smile) - `MOUTH_SMILE_L`, `MOUTH_SMILE_R`
- **AU14** (Dimple) - `MOUTH_DIMPLE_L`, `MOUTH_DIMPLE_R`
- **AU15** (Mouth Frown) - `MOUTH_FROWN_L`, `MOUTH_FROWN_R`
- **AU16** (Lower Lip Depress) - `MOUTH_DOWN_LOWER_L`, `MOUTH_DOWN_LOWER_R`
- **AU17** (Mouth Shrug Lower) - `MOUTH_SHRUG_LOWER`
- **AU18** (Mouth Pucker) - `MOUTH_PUCKER_UP_L`, `MOUTH_PUCKER_UP_R`
- **AU20** (Mouth Stretch) - `MOUTH_STRETCH_L`, `MOUTH_STRETCH_R`
- **AU21** (Mouth Tighten) - `MOUTH_STRETCH_L`, `MOUTH_STRETCH_R` (same as AU20)
- **AU22** (Mouth Funnel) - `MOUTH_FUNNEL_UP_L`, `MOUTH_FUNNEL_UP_R`
- **AU23** (Mouth Tighten) - `MOUTH_TIGHTEN_L`, `MOUTH_TIGHTEN_R`
- **AU24** (Mouth Press) - `MOUTH_PRESS_L`, `MOUTH_PRESS_R`
- **AU25** (Lips Parted) - `IH` (viseme shape)
- **AU26** (Jaw Drop) - `JAW_OPEN` (small)
- **AU27** (Mouth Stretch) - `JAW_OPEN` (large)
- **AU28** (Lips Suck) - `MOUTH_ROLL_IN_*`

---

## Primary Confounding Situations

### 1. **JAW_OPEN Conflicts** (CRITICAL)
**Problem**: Both visemes and AUs drive jaw opening, but with different semantics.

**Viseme Jaw Logic** (from `LateUpdate()`):
```
jawOpen = MAX(
  AE=1.0,      // widest
  AH=1.0,      // wide
  OH=0.8,      // medium
  W_OO=0.6,    // small
  TH=0.2,      // minimal
  IH=0.2,      // minimal
  EE=0.2       // minimal
)
```
- Converted to ~10° jaw rotation via `Quaternion.Euler(0, 0, -jawAngleInc)`

**AU Jaw Logic**:
- **AU26** (Mouth Parted): Subtle jaw opening (10% weight)
- **AU27** (Mouth Stretched Open): Significant jaw opening (20% weight)

**Confounding**:
- If saying "AH" vowel (viseme), jaw opens ~10° naturally
- If AU27 is applied simultaneously (surprise/fear), both systems fight for the same rotationvalue
- No blending mechanism—last-write wins unpredictably
- **Example**: Exclamation "Ah!" with surprise: jaw may open inconsistently

---

### 2. **Mouth Pucker-Viseme Conflicts** (HIGH)
**Problem**: AU18 (Mouth Pucker) uses blendshapes that conflict with vowel pronunciation.

**AB18 Output**:
- Activates `MOUTH_PUCKER_UP_L`, `MOUTH_PUCKER_UP_R` (lips rounded/pursed)

**Conflicting Visemes** (from `FixConfoundingKeys()`):
- `AH`, `AE` (wide open vowels)
- These suppress puckering when active (suppression factor = `1 - wAH` or `1 - wAE`)

**Confounding Situations**:
- Speaking "ah" while expressing contempt/scorn (AU18) → lips forced to pucker, distorting vowel
- Suppression logic explicitly prevents both simultaneously, leading to unnatural expression
- Emotion overtakes phoneme fidelity

**Example Scenario**:
```
Viseme: AH (wide open)
AU18: Scorn/disdain → Pucker
Result: Viseme suppressed by AU, "A" sounds become pinched, unnatural
```

---

### 3. **Mouth Funnel Contradictions** (HIGH)
**Problem**: AU22 (Mouth Funnel) conflicts with both open and puckered mouth phonemes.

**AU22 Output**:
- `MOUTH_FUNNEL_UP_L`, `MOUTH_FUNNEL_UP_R` (lips drawn inward & funneled)

**Conflicting Visemes**:
- **With AE (wide)**: Explicitly suppressed (factor = `1 - maxWide`)
- **With B_M_P**: Also suppressed (factor = `1 - wBMP`)
- **With F_V**: `MOUTH_FUNNEL_DOWN_*` suppressed instead

**Confounding**:
- AU22 naturally suppresses wide vowels (AE, AH)
- "Sadness/worry" emotion (funnel) incompatible with saying "ah" or "ay"
- Forced suppression causes lip movement artifacts

**Example**:
```
Emotion: Sadness (AU22 funnel)
Speech: "bay" (B_M_P + AE)
Result: AE suppressed to preserve funnel → "b__" (vowel weakened)
```

---

### 4. **Mouth Shrug Conflicts** (MEDIUM)
**Problem**: AU10/AU17 (Mouth Shrug) conflicts with viseme execution.

**AU10 (Upper Shrug)**:
- `MOUTH_SHRUG_UPPER` tensionas upper lip
- Explicitly suppressed by `B_M_P` viseme (factor = `1 - wBMP`)

**AU17 (Lower Shrug)**:
- `MOUTH_SHRUG_LOWER` tenses lower lip
- Explicitly suppressed by `EE` viseme

**Confounding**:
- Shrugging (shoulder/lip uncertainty) requires tension in mouth
- But pronouncing bilabials (B, M, P) requires relaxation for lip closure
- Speaking "bye" or "meh" while shrugging produces unnatural restriction

---

### 5. **Fricative-Tuck Interdependency** (MEDIUM)
**Problem**: AU23 (Mouth Tighten) and F_V viseme create competitive effects.

**F_V (Fricative)**:
- Requires taut lips for air passage (S-sound)
- Actively suppresses:
  - `MOUTH_FUNNEL_DOWN_*` (undermines fricative)
  - `MOUTH_SHRUG_LOWER`
  - `MOUTH_DOWN_LOWER_*` (lower lip must stay tense)

**AU23 (Tighten)**:
- `MOUTH_TIGHTEN_L`, `MOUTH_TIGHTEN_R` (tensed lips)
- At maximum intensity, reduces blendshape ranges

**Confounding**:
- Both F_V and AU23 seek to tighten lips, but:
  - F_V needs specific phonetic shape
  - AU23 creates emotional expression (tension/determination)
- Combined effect = over-tightened, unnatural appearance
- No prioritization mechanism

---

### 6. **Dental-Fricative Overlap** (MEDIUM)
**Problem**: TH and S_Z share similar articulation space but different AU profiles.

**TH Viseme**:
- Tongue between teeth
- Moderate jaw opening (20% via viseme)

**S_Z Viseme**:
- Close fricative (no tongue placement constraint)
- Minimal jaw opening (5% via viseme effect)

**Shared Tension**:
- Both compete for fine mouth shape control
- If AU24 (Mouth Press) applied during TH → over-constriction
- If AU18 (Pucker) applied during S_Z → de-tuning of fricative

---

### 7. **Jaw Rotation vs. AE/AH Vowel Conflict** (MEDIUM)
**Problem**: Jaw rotation-driven by viseme weights can conflict with AU-driven jaw positioning.

**Current Logic**:
```csharp
float jawOpen = MAX(AE, AH);  // 1.0 each
_jawRot = _jawRotInit * Quaternion.Euler(0, 0, -jawAngleInc);  // ~10° rotation
```

**AU26/AU27 Create Same Effect**:
- AU26: 10% JAW_OPEN weight
- AU27: 20% JAW_OPEN weight

**Confounding**:
- No addition—just replacement
- If viseme says "AH" (10° jaw), then AU27 applied, no cumulative rotation
- If AU27 expires but "AH" continues, jaw snaps back—discontinuity
- Timing mismatches between viseme transitions and AU intensity curves

---

### 8. **Smile-Vowel Interaction** (LOW-MEDIUM)
**Problem**: AU12 (Smile) affects mouth corners, creating secondary effects on vowel shapes.

**AU12 Output**:
- `MOUTH_SMILE_L`, `MOUTH_SMILE_R` (corners pulled back)

**Secondary Effect**:
- Smiling narrows interpupillary width
- Wide vowels (AE, AH) require mouth width
- Smile reduces effective jaw opening perception

**Confounding**:
- "Smile while speaking 'ah'" appears less open than "expressionless while speaking 'ah'"
- Not explicitly handled in FixConfoundingKeys()
- Creates subtle but persistent visual oddity

---

### 9. **Mouth Stretch Ambiguity** (MEDIUM)
**Problem**: AU20/AU21 (Mouth Stretch) conflict with viseme mouth width.

**AU20/AU21 Output**:
- `MOUTH_STRETCH_L`, `MOUTH_STRETCH_R`
- Creates extreme lateral stretch (grin/grimace)

**Conflicting Viseme**:
- `EE` (smile vowel) also widens mouth laterally
- Both drive similar parameters, but:
  - EE is phonetic (necessary for correct pronunciation)
  - AU20/21 is emotional (exaggerated grin)

**Confounding**:
- Saying "E" sound while grimacing (AU20) → double-wide mouth
- Overdone, unnatural appearance
- No suppression mechanism between AU20/21 and EE

---

### 10. **R Rhotic & Jaw Coordination Undefined** (LOW)
**Problem**: R viseme has complex jaw-tongue interaction not modeled in jaw logic.

**R Viseme**:
- Tongue position critical
- Current system treats as blendshape only
- Minimal jaw effect (not in jaw logic equation)

**Missing AU Coordination**:
- If AU26 (jaw parted) happens during R → jaw and tongue misaligned
- R requires precise oral cavity shape; AU disrupts it

---

### 11. **Asynchronous Blend Weighting** (CRITICAL - SYSTEM-WIDE)
**Problem**: No unified weight-blending between visemes and AUs.

**Current Logic**:
1. Visemes: Normalized weights (each viseme 0-1)
2. AUs: Direct blendshape application (0-100)
3. **Interaction**: 
   - Visemes applied first via `GetCurrentNormalizedVisemeWeights()`
   - AUs applied afterward via `AnimateAU()`
   - Most-recently-applied value wins per blendshape

**Confounding**:
- No priority system
- No blending between simultaneous viseme+AU on same blendshape
- Rapid timing changes cause flickering/jittering
- Example: While speaking, brief AU triggers may erase part of mouth shape

---

## Suppression Logic (Current Mitigation)

The system implements `FixConfoundingKeys()` with explicit suppression:

### B_M_P (Bilabials)
Suppresses:
- `MOUTH_SHRUG_UPPER` (0% weight)
- `MOUTH_FUNNEL_UP_L`, `MOUTH_FUNNEL_UP_R`
- `MOUTH_DOWN_LOWER_L`, `MOUTH_DOWN_LOWER_R`
- `MOUTH_FROWN_L`, `MOUTH_FROWN_R`

**Rationale**: Bilabials require closed mouth; other expressions interfere.

### EE (Close Vowel)
Suppresses:
- `MOUTH_SHRUG_LOWER`

**Rationale**: EE widens mouth; shrug would narrow it.

### AE/AH (Wide Vowels)
Suppresses:
- `MOUTH_PUCKER_UP_L`, `MOUTH_PUCKER_UP_R`
- `MOUTH_FUNNEL_UP_L`, `MOUTH_FUNNEL_UP_R`
- `MOUTH_TIGHTEN_L`, `MOUTH_TIGHTEN_R`

**Rationale**: Wide vowels incompatible with pursed/funneled/tightened lips.

### F_V (Fricative)
Suppresses:
- `MOUTH_FUNNEL_DOWN_L`, `MOUTH_FUNNEL_DOWN_R`
- `MOUTH_SHRUG_LOWER`
- `MOUTH_DOWN_LOWER_L`, `MOUTH_DOWN_LOWER_R`

**Rationale**: F/V require specific lip shape; suppression preserves articulation.

---

## Gaps in Current Suppression Logic

### Missing Suppressions (Problematic Interactions):
1. **AU22 (Funnel) & B_M_P (Bilabial)** → NOT suppressed
   - Can occur together; creates over-constrained mouth
   
2. **AU18 (Pucker) & Fricatives (S_Z, F_V)** → NOT suppressed
   - Puckering de-tunes fricatives
   
3. **AU20/AU21 (Stretch) & EE Vowel** → NOT suppressed
   - Double-widening creates unnatural expression
   
4. **AU27 (Jaw Open) & Fricatives (S_Z)** → NOT suppressed
   - Large jaw opening disrupts fricative closure
   
5. **AU24 (Press) & Open Vowels (AE, AH)** → NOT suppressed
   - Pressing undermines open vowel phonetic fidelity

6. **AU15 (Frown) & Smile Vowel (EE)** → NOT suppressed
   - Frown with EE creates contradictory emotion

---

## Specific Problem Scenarios

### Scenario 1: Excited Greeting
```
Speech: "Oh my!" (OH, AH, vowel transitions)
Emotion: Surprise (AU5 eye wide + AU27 mouth open)
Problem: AU27 jaw opening combines with AH viseme jaw opening
         → Over-extended jaw, unnatural appearance
         → No coordinated blending
Expected: Natural "Oh" with surprise-congruent jaw position
```

### Scenario 2: Angry Affirmation
```
Speech: "Yes!" (EE vowel to consonant transition)
Emotion: Anger (AU4 brow lower + AU23 mouth tighten)
Problem: AU23 tightens lips while EE needs wide mouth
         → EE vowel suppressed by AU tightness
         → "Yes" sounds pinched/strangled
Expected: Strong "Yes!" with anger-congruent jaw tension, not suppressed phoneme
```

### Scenario 3: Sad Whisper
```
Speech: "I'm sorry" (vowels and fricatives)
Emotion: Sadness (AU22 mouth funnel)
Problem: AU22 suppresses vowels (AH, AE, etc.)
         → "I'm" → "I_m" (vowel weakened)
         → "sorry" → "s_rry" (fricative + funnel conflict)
Expected: Clear speech with subtle sadness lip shape
```

### Scenario 4: Nervous Laughter
```
Speech: "Ha ha" (ER, AH repeated)
Emotion: Nervousness (AU25 lips parted + AU17 lower shrug)
Problem: AU17 shrug suppresses EE (not spoken, so ok)
         BUT: AU25 (lips parted) uses IH blendshape
         → No conflict, but IH partially blocks AH vowel weights
         → Laugh sounds forced/unnatural
Expected: Relaxed laugh with subtle nervousness
```

### Scenario 5: Flirty Playfulness  
```
Speech: "Really?" (vowel transitions E→AE→EE)
Emotion: Playfulness (AU12 smile + AU18 subtle pucker)
Problem: AU18 actively suppresses AE and AH
         → "Really" → "R__lly" (mid-vowel suppressed)
         → Smile (AU12) makes mouth corners back, reducing apparent AE width
         → Word sounds strange/incomplete
Expected: Clear "Really?" with cute lip shape
```

---

## Quantified Overlap Matrix

| Viseme | Blendshapes | AU_Conflicts | Severity |
|--------|-------------|-------------|----------|
| B_M_P | Close mouth | AU10, AU22, AU14 | HIGH |
| F_V | Lip tighten | AU18, AU22, AU24 | MEDIUM |
| TH | Dental | AU24 (press) | LOW |
| T_L_D_N | Tongue position | AU24, AU27 | LOW |
| K_G_H_NG | Velar constriction | AU27 | LOW |
| CH_J | Affricate | AU18, AU22 | MEDIUM |
| S_Z | Alveolar fricative | AU18, AU23, AU27 | MEDIUM |
| R | Rhotic | AU26, AU27 | LOW |
| AH | Wide open | AU18, AU22, AU24, AU23 | CRITICAL |
| EE | Smile vowel | AU15, AU20 | MEDIUM |
| IH | Parted lips | AU25 (redundant), AU24 | LOW |
| OH | Mid-open | AU18, AU22 | MEDIUM |
| W_OO | Rounded | AU18, AU22 | MEDIUM |
| AE | Wide/flat | AU18, AU22, AU23 | CRITICAL |

**Key Insight**: **AE and AH vowel phonemes have the most conflicts** with emotional AUs, as they are the widest mouth shapes and emotionally positive vowels.

---

## Recommendations for Resolution

### 1. **Implement Unified Weight Blending**
Instead of last-write-wins, use:
```csharp
float visemeWeight = GetVisemeBlendValue(blendshapeName);
float auWeight = GetAUBlendValue(blendshapeName);
float finalWeight = Blend(visemeWeight, auWeight);  // Weighted average or max
```

### 2. **Add AU Suppression Matrix**
```csharp
// AU → Visemes that suppress/are suppressed by
suppressionMatrix[AU22] = { AH, AE, EE };  // Funnel suppresses wide vowels
suppressionMatrix[AU18] = { AH, AE, F_V, S_Z };  // Pucker suppresses fricatives
suppressionMatrix[AU27] = { S_Z, TH };  // Large jaw open disrupts fricatives
```

### 3. **Jaw Rotation Coordination**
Add cumulative jaw rotation for AUs:
```csharp
float visemeJawAngle = ComputeVisemeJawAngle();
float auJawAngle = ComputeAUJawAngle();
float blendedJawAngle = Blend(visemeJawAngle, auJawAngle);
```

### 4. **Priority System**
Define priority levels:
- **P0 (Critical)**: Speech clarity (visemes take precedence)
- **P1 (Emotional)**: Expression congruence (AUs secondary)
- **P2 (Aesthetic)**: Visual appeal (blended suppressions)

### 5. **Explicit Conflict Zones**
Flag high-conflict phoneme-AU pairs and use alternative representations:
- AU22 + AH → Substitute subtle FV instead of AH
- AU18 + S_Z → Reduce AU18 intensity during S_Z
- AU27 + Fricatives → Limit jaw rotation during fricative windows

### 6. **Temporal Smoothing**
Add blend curves during transitions:
```csharp
// Smooth transition between viseme and AU-driven values
float transitionAlpha = Mathf.Clamp01((currentTime - lastChangeTime) / 0.1f);
float smoothedValue = Lerp(previousValue, targetValue, transitionAlpha);
```

---

## Summary Table: Critical Confoundings

| # | Viseme | AU | Issue | Severity | Current Mitigation |
|---|--------|----|----|----------|-------------------|
| 1 | AH / AE | AU18 (Pucker) | Suppression active | HIGH | `ApplySuppression()` |
| 2 | AH / AE | AU22 (Funnel) | Suppression active | HIGH | `ApplySuppression()` |
| 3 | AE | AU23 (Tighten) | Suppression active | HIGH | `ApplySuppression()` |
| 4 | AE | AU20 (Stretch) | Double-widening | MEDIUM | None—gap |
| 5 | EE | AU15 (Frown) | Emotional contradiction | MEDIUM | None—gap |
| 6 | EE | AU20 (Stretch) | Over-wide mouth | MEDIUM | None—gap |
| 7 | F_V | AU23 (Tighten) | Over-tightened fricative | MEDIUM | None—gap |
| 8 | S_Z | AU18 (Pucker) | De-tuned fricative | MEDIUM | None—gap |
| 9 | All | AU26/AU27 | Jaw rotation timing | CRITICAL | Last-write-wins (poor) |
| 10 | B_M_P | AU22 (Funnel) | Over-constrained mouth | MEDIUM | None—gap |


---

## Conclusion

The CC4 character + OVRLipSync + FACS system exhibits **three orders of confounding**:

1. **System-level**: Lack of unified weighting between viseme and AU blendshape drives
2. **Phoneme-level**: Specific visemes incompatible with specific AUs (table above)
3. **Temporal-level**: No coordination of viseme-AU transitions, causing jitter/discontinuities

The current `FixConfoundingKeys()` method addresses ~40% of conflicts through explicit suppression, but leaves significant gaps, especially around:
- Jaw rotation coordination
- Fricatives under emotion
- Wide vowels under facial expression
- Temporal smoothing across systems

**Priority fixes**: Implement unified weight blending and address the 10 critical conflicts identified above.
