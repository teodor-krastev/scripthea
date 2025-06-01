### Scripthea

#### Intro

Scripthea is a free, open-source Windows application designed to streamline the process of crafting prompts for text-to-image AI generators like Stable Diffusion. Developed by Teodor Krastev, Scripthea offers a structured environment for building, testing, and refining prompts, making it an invaluable tool for artists, designers, and AI enthusiasts seeking greater control over their creative outputs. A short introduction video is in https://youtu.be/BSUsCHmuI0Y .

At its core, Scripthea simplifies prompt engineering by breaking down prompts into two components: cues (descriptive phrases or subjects) and modifiers (attributes like style, lighting, or artist references). This modular approach allows users to experiment with various combinations, facilitating a more systematic exploration of visual styles and themes.

#### Here is the prompt composer tab...
![Scripthea-1.png](/docs/Scripthea-1.jpg)

On the left, you see the log panel which will text you about any ongoing operations. For prompt composing, there are two modes: Single and Scan. In Single mode, you can use one cue and more than one modifier. In Scan mode, you can select any number of cues and any number of modifiers but each prompt will combine only one cue with one modifier. Modifiers are divided into categories and to use modifiers from any category you need to check the category itself. If you wonder about any modifier, hover over it, there will be a hint for some of them. If you right-click on any modifier you will be asked to confirm a google search for that modifier. In options, you can specify the image depot folder where the images from your scan (or single query) will go.

#### ...and here it is the image depot viewer
![grid-view.png](/docs/viewer-grid.jpg)

The viewer shows a Scripthea image depot (a folder with bunch of images and decription.txt file for the prompts). You can chose between table view and thumbnail (grid) view. In the grid view you can adjust the thumbnails from the menu (bottom left button). You can move around with the arrows on the bottom, all self-explanatory (I think). On the very bottom common for both vews is the find panel which will find a word(s) in the prompts of the active image depot and select it.
The image itself can be zoomed in/out(buttons), panned (scroll-bars) or fit (the middle button), more tools are planned...

#### Utilities
The third tab is Image Depot Master as Image Depot manager - copy, move, delete images, check for consistency, etc 

The fourth tab of Scripthea contains a utility of converting files from craiyon.com generator (ex Dall-E mini). The images from there have the prompt as file name or at least part of it. The Scripthea utility will convert these images in Scripthea image depot.

The description.idf  file is a text file where each line is json formatted dictionary of a image properties in the same folder. 
All the options, external and internal sizes and position are saved on closing and retrieved. 

#### need HELP or just run it
Wherever you are within the application press F1 for online help (https://scripthea.com)
If you have concerns about viruses, compile from the sources - I'm using C#, .NET framework 4.8 with WPF and Visual Studio 2019 as IDE.

#### contact 
Keep in mind that the application is under active development, so let me know about any bug at https://sicyon.com/survey/comment.html and I'll do my best to fix it asap. In the same way, you can communicate any ideas for improvement, experiences with the software or your willingness to help me with the project. I would especially appreciate more prompt collections preferably organized by subject, see config folder for *.prompts files.

#### legal
Scripthea software has been written by and is copyrighted to Teodor Krastev. The sources are distributed under MIT's open source license. 
