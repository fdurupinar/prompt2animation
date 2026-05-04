import sys
import numpy as np
from sklearn.cluster import KMeans
from au_utils import *
import matplotlib.pyplot as plt

# Parameters
n_samples = 110   # number of vectors
n_features = 65   # dimensionality
n_clusters = 22    # number of clusters

# filenames, X_dense = read_aus("Generated")
filenames, X_dense = read_aus("../Assets/Resources/OCC-Gemini")
X_dense = np.transpose(X_dense)

kmeans = KMeans(n_clusters=n_clusters, random_state=0)
kmeans.fit(X_dense)

# Output cluster labels
labels = kmeans.labels_

# Display results
#print("Cluster assignments for first 10 samples:", labels[:10])
print("Cluster centers shape:", kmeans.cluster_centers_.shape)
print("Inertia (sum of squared distances):", kmeans.inertia_)

ave_faces = np.zeros((n_clusters, n_features))
for i in range(n_clusters):
    print("CLUSTER ", i)
    for j in range(len(labels)):
        clusterid = labels[j]
        if clusterid == i:
            print("  %s"%filenames[j])
            for k in range(n_features):
                ave_faces[i][k] = ave_faces[i][k] + X_dense[j][k]

for i in range(n_clusters):
    num_elements = np.sum(labels == i)
    for k in range(n_features):
        ave_faces[i][k] = ave_faces[i][k] / num_elements

for i in range(n_clusters):
    csvfilename = "cluster-aveface-%d.csv"%(i)
    print(csvfilename)
    write_aus_csv(csvfilename, X_dense)
 
sorted_indices = np.argsort(labels)
X_sorted = X_dense[sorted_indices]
names_sorted = np.array(filenames)[sorted_indices]

au_names = read_au_names("au_names.txt")

px = 1/plt.rcParams['figure.dpi']  # pixel in inches
plt.figure(figsize=( 1000* px, 800 * px)) 
plt.imshow(X_sorted, interpolation='nearest', aspect='auto')
plt.yticks(ticks=np.arange(X_sorted.shape[0]), labels=names_sorted, fontsize=4)
plt.xticks(ticks=np.arange(X_sorted.shape[1])+1, labels=au_names, fontsize=4, rotation=-45)
ax = plt.gca()
x = 0
for i in range(n_clusters):
    num_elements = np.sum(labels == i)
    x = x + num_elements
    ax.axhline(x - 0.5, color='black', linewidth=1)
    print("cluster %d has %d elements"%(i, num_elements))

plt.show()