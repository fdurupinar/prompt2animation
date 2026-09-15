"""Local WAV + known English transcript -> aligned ARPAbet phonemes; no viseme mapping."""
import json, math, re, sys, warnings
from pathlib import Path

def align(request):
    import numpy as np
    from scipy.io import wavfile
    from scipy.signal import resample_poly
    from pocketsphinx import Decoder
    with warnings.catch_warnings():
        warnings.simplefilter('ignore', wavfile.WavFileWarning)
        rate, samples = wavfile.read(request['audio'])
    if not len(samples):
        raise ValueError('The audio is empty.')
    duration = len(samples) / rate
    if samples.dtype.kind == 'u':
        samples = (samples.astype(float) - 128) / 128
    elif samples.dtype.kind == 'i':
        samples = samples.astype(float) / (2 ** (samples.dtype.itemsize * 8 - 1))
    else:
        samples = samples.astype(float)
    if samples.ndim == 2:
        samples = samples.mean(axis=1)
    if not np.isfinite(samples).all() or np.max(np.abs(samples)) < 1e-7:
        raise ValueError('The audio is silent or contains invalid samples.')
    divisor = math.gcd(rate, 16000)
    pcm = resample_poly(samples, 16000 // divisor, rate // divisor)
    pcm = (np.clip(pcm, -1, 1) * 32767).astype('<i2').tobytes() + bytes(6400)
    transcript = request['transcript'].replace('’', "'").lower().strip()
    if any(ord(c) > 127 and c.isalnum() for c in transcript):
        raise ValueError('The bundled alignment model supports English transcripts only.')
    words = re.findall(r"[a-z0-9]+(?:'[a-z0-9]+)*", transcript)
    if not words:
        raise ValueError('The transcript contains no words.')
    decoder = Decoder(samprate=16000, loglevel='ERROR')
    for word, pronunciation in request.get('pronunciations', {}).items():
        decoder.add_word(word.lower(), pronunciation)
    unknown = sorted({word for word in words if decoder.lookup_word(word) is None})
    if unknown:
        raise ValueError('No pronunciation for: ' + ', '.join(unknown) +
                         '. Supply ARPAbet pronunciations or write numbers as spoken words.')
    sentinel = 'codexalignmentendpad'
    if sentinel in words:
        raise ValueError('Transcript contains a reserved alignment token.')
    decoder.add_word(sentinel, 'SIL')
    decoder.set_align_text(' '.join(words + [sentinel]))
    for pass_index in range(2):
        if pass_index:
            decoder.set_alignment()
        decoder.start_utt()
        decoder.process_raw(pcm, full_utt=True)
        decoder.end_utt()
    word_intervals, phones, aligned_words = [], [], []
    for word in decoder.get_alignment():
        name = re.sub(r'\(\d+\)$', '', word.name)
        if name == sentinel:
            continue
        if not name.startswith('<'):
            aligned_words.append(name)
            if word.duration <= 0 or word.start / 100 >= duration:
                raise ValueError('A transcript word could not be aligned within the original audio: ' + name)
            word_intervals.append(dict(word=name, start=word.start / 100,
                                       end=min(duration, (word.start + word.duration) / 100)))
        for phone in word:
            start, end = phone.start / 100, (phone.start + phone.duration) / 100
            if phone.name != 'SIL' and end > duration + .02:
                raise ValueError('Speech alignment extends beyond the original audio.')
            end = min(end, duration)
            if start < end:
                phones.append(dict(phone=phone.name, start=start, end=end))
    if aligned_words != words:
        raise ValueError('Alignment did not preserve every transcript word in order.')
    if not phones or not any(p['phone'] != 'SIL' for p in phones):
        raise ValueError('No speech phonemes were aligned.')
    for previous, current in zip(phones, phones[1:]):
        if current['start'] < previous['end']:
            raise ValueError('Alignment produced overlapping phonemes.')
    return dict(duration=duration, phonemes=phones, words=word_intervals,
                method='PocketSphinx forced alignment; estimated acoustic boundaries')

if __name__ == '__main__':
    try:
        result = align(json.loads(Path(sys.argv[1]).read_text(encoding='utf-8')))
        print(json.dumps(result, allow_nan=False))
    except Exception as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
