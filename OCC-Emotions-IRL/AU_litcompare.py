import csv, sys, json
import numpy as np
from au_utils import *
from FACS_csv2json import openface_csv2json
import matplotlib.pyplot as plt

def process_csv_emotions(filename):
    expressions = {}
    with open(filename, newline='', encoding='utf-8') as csvfile:
        header = [h.strip() for h in csvfile.readline().split(',')]
        reader = csv.DictReader(csvfile, fieldnames=header)
        for value in reader:
            name = value['\ufeffName']
            aus = []
            austrings = value['AUs'].split(' ')
            for austring in austrings:
                if austring != '':
                    au = int(austring)
                    aus.append(au)
            expressions[name] = aus
    return expressions

def expand_aus(aus):
    all_aus = np.zeros((65,1))
    for au in aus:
        all_aus[au] = 80 # 80% intensity
    return all_aus

def export(expressions):
    # put one expression per csv
    for name, aus in expressions.items():
        filename = name + ".csv"
        all_aus = expand_aus(aus)
        write_aus_csv(filename, all_aus)
        openface_csv2json(name, 20, filename)

def is_subset(array1, array2) -> int:
    """Return the percentage of elements from array 1 that are in array 2."""
    dist_counter = 0
    for n in range(len(array1)):
        if array2[array1[n]] > 0:
            dist_counter += 1
    return (dist_counter)/len(array1)
        
def compare(filenames, X_dense, expressions, expressionName, eindex, data):
    print(expressionName)
    for i in range(X_dense.shape[0]):
        # what theoretical expression is closest?
        novel_aus = expressions[expressionName]
        #print(X_dense[i])
        novel = is_subset(novel_aus, X_dense[i]) # each row is an expression
        print(" ", filenames[i], novel)
        data[i][eindex] = novel
        
def compare_appraisal_closest(filenames, X_dense, expressions, data):
    for i in range(X_dense.shape[0]):
        # what theoretical expression is closest?
        novel_aus = expressions["Novel"]
        novel = is_subset(novel_aus, X_dense[i]) # each row is an expression
        if novel > 0.5:
            print(" ", filenames[i], "-> NOVEL")
            data[i][0] = 1.0
        else:
            print(" ", filenames[i], "-> NOT NOVEL")
            data[i][0] = 0.0

        pleasant = is_subset(expressions["Pleasant"], X_dense[i])
        notpleasant = is_subset(expressions["Not-Pleasant"], X_dense[i])
        if pleasant > notpleasant and pleasant > 0.3:
            print(" ", filenames[i], "-> Pleasant")
            data[i][1] = 1.0
        elif pleasant < notpleasant and notpleasant > 0.3:
            print(" ", filenames[i], "-> NOT Pleasant")
            data[i][1] = 0.0
        else:
            print(" ", filenames[i], "-> NEUTRAL ")
            data[i][1] = 0.5

        coping_nocontrol = is_subset(expressions["Coping-NoControl"], X_dense[i])
        coping_highpower = is_subset(expressions["Coping-Control-HighPower"], X_dense[i])
        coping_lowpower = is_subset(expressions["Coping-Control-LowPower"], X_dense[i])
        print(coping_nocontrol, coping_highpower, coping_lowpower)
        if coping_nocontrol > coping_highpower and coping_nocontrol > coping_lowpower:
            print(" ", filenames[i], "-> Coping No Control")
            data[i][2] = 0.0
        elif coping_highpower > coping_nocontrol and coping_highpower > coping_lowpower:
            print(" ", filenames[i], "-> Coping Control High power")
            data[i][2] = 1.0
        else:
            print(" ", filenames[i], "-> Coping Control Low power")
            data[i][2] = 0.5


def compare2(filenames, X_dense, expressions):
    threshold = 0.5
    for i in range(X_dense.shape[0]):
        # what theoretical expression is closest?
        print(filenames[i])
        for name, aus in expressions.items(): 
            novel_aus = expressions[name]
            novel = is_subset(novel_aus, X_dense[i]) # each row is an expression
            if novel > threshold:
                print(" ", name, novel)

def Scherer_compare(filenames, X_dense):
    expressions = process_csv_emotions("Scherer-Appraisal.csv")
    #export(expressions) # for visualization
    # visualize percent overlap with action units
    #data = np.zeros((110, 7))
    #appraisal_names = ["Novel", "Pleasant", "Not-Pleasant", "Inconsistent", "Coping-NoControl", "Coping-Control-HighPower", "Coping-Control-LowPower"]
    #for i in range(len(appraisal_names)):
        #name = appraisal_names[i]
        #compare(filenames, X_dense, expressions, name, i, data)
    #px = 1/plt.rcParams['figure.dpi']  # pixel in inches
    #plt.figure(figsize=( 1000* px, 800 * px)) 
    #plt.imshow(data, interpolation='nearest', aspect='auto')
    #plt.yticks(ticks=np.arange(data.shape[0]), labels=filenames, fontsize=4)
    #plt.xticks(ticks=np.arange(data.shape[1])+0.5, labels=appraisal_names, fontsize=4, rotation=-45)
    #ax = plt.gca()
    #for x in range(5,data.shape[0], 5):
    #    ax.axhline(x - 0.5, color='black', linewidth=1)
    #plt.show()

    # Compute what novelty, pleasantness, and coping style is closest
    data = np.zeros((110, 3))
    compare_appraisal_closest(filenames, X_dense, expressions, data)
    px = 1/plt.rcParams['figure.dpi']  # pixel in inches
    plt.figure(figsize=( 1000* px, 800 * px)) 
    plt.imshow(data, interpolation='nearest', aspect='auto')
    plt.yticks(ticks=np.arange(data.shape[0]), labels=filenames, fontsize=4)
    plt.xticks(ticks=np.arange(data.shape[1])+0.5, labels=["Novelty", "Pleasantness", "Control-Power"], fontsize=4, rotation=-45)
    ax = plt.gca()
    for x in range(5,data.shape[0], 5):
        ax.axhline(x - 0.5, color='black', linewidth=1)
    plt.show()

def cluster(filenames, X_dense, expressions):
    label_names = ["Happiness", "Anger", "Disgust", "Sadness", "Surprise", "Fear"]
    n_clusters = len(label_names)
    centers = []
    for label in label_names:
        #values = expand_aus(expressions[label])
        #centers.append(values)
        centers.append(expressions[label])

    labels = np.zeros(110)
    for i in range(X_dense.shape[0]):
        mindist = sys.float_info.max
        best_label = -1
        for j in range(len(label_names)):
            center = centers[j]
            dist = is_subset(center, X_dense[i]) #np.linalg.norm(center-X_dense[i])
            if dist < mindist:
                mindist = dist
                best_label = j
        labels[i] = best_label
    
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
            
    #plt.figure(figsize=( 1000* px, 800 * px)) 
    #plt.imshow(centers, interpolation='nearest', aspect='auto')
    #plt.yticks(ticks=np.arange(len(centers)), labels=label_names)
    #plt.xticks(ticks=np.arange(len(centers[0]))+1, labels=au_names, fontsize=4, rotation=-45)
    #plt.show()

filenames, X_dense = read_aus("Generated")
X_dense = np.transpose(X_dense)
# Scherer_compare(filenames, X_dense)

expressions = process_csv_emotions("Emotions-AUs.csv")
#export(expressions) # for visualization
#compare2(filenames, X_dense, expressions)
cluster(filenames, X_dense, expressions)


