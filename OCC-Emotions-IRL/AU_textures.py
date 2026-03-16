#! /bin/python

import json,os,sys
import numpy as np
import matplotlib.pyplot as plt
from au_utils import *

au_names = read_au_names("au_names.txt")
filenames, aus = read_aus("Generated")

names = []
for name in filenames:
    if "3" in name: 
        names.append(name[:-1])
    else: 
        names.append("")

fig, ax = plt.subplots()
im = ax.matshow(aus)

#Set tick positions to center of each pixel
ax.set_xticks(np.arange(aus.shape[1]))
ax.set_yticks(np.arange(aus.shape[0]))

# Set tick labels
print(names)
ax.set_xticklabels(names)
ax.set_yticklabels(au_names)

# Center tick labels
ax.tick_params(axis='x', labelrotation=45)
ax.tick_params(axis='both', which='both', length=0)  # remove tick lines if you want a cleaner look

# Optional: move ticks to top
ax.tick_params(top=True, bottom=False, labeltop=True, labelbottom=False, labelsize=6)

for x in range(5, aus.shape[1], 5):
    ax.axvline(x - 0.5, color='black', linewidth=1)

#plt.xticks(ticks=np.arange(aus.shape[1]), labels=names, rotation=45, fontsize=6)
#plt.yticks(ticks=np.arange(aus.shape[0]), labels=au_names, fontsize=6)
plt.title("Comparison of action units")
plt.xlabel("Generated Animation")
plt.ylabel("Atcion Unit")
plt.show()
