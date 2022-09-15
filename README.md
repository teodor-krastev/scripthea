### Scripthea

#### Intro

Scripthea is my response to the booming development of text-to-image domain. It's an attempt to apply a more systematic approach in composing the text prompt (cue, here). You will be offered collections of short descriptive texts and collections of modifiers, like a painter, art style, time period, etc.
After composing your cue you can copy and paste it into your favourite text-to-image generator and see to result in your browser. 
Alternatively, you can use API access to some generators (just DeepAI for now) and here is the strong side of Scripthea. The cue and the picture become part of your collection (image depot) with a convenient image viewer on the second tab. On top of that, you can "scan". After selecting some texts and some modifiers Scan will combine them as every text will be combined with every modifier. So for example if you would like to see how a particular painter would paint different subjects or how specific topic would be painted by different painters. Scripthea will generate all the combinations for you (scan) and query the active API for you to put them in an image collection.

#### Here is the cue composer tab...
![PenPic-1.png](/docs/PenPic-1.png)

On the left, you see the log panel which will text you about any ongoing operations. For cue composing, there are two modes: Single and Scan. In Single mode, you can use one text and more than one modifier. In Scan mode, you can select any number of texts and any number of modifiers but each cue will combine only one text with one modifier. Modifiers are divided into categories and to use modifiers from any category you need to check the category itself. If you wonder about any modifier, hoover over it, there will be a hint for some of them. If you right-click on any modifier you will be asked to confirm a google search for that modifier. In options, you can specify the image depot folder where the images from your scan (or single query) will go.

#### ...and here it is the image depot viewer
![PenPic-2.png](/docs/PenPic-2.png)

The viewer shows a Scripthea image depot (a folder with bunch of images and decription.txt file for the cues). You can chose between table view and thumbnail (grid) view. In the grid view you can adjust the thumbnails from the menu (bottom left button). You can move around with the arrows on the bottom, all self-explanatory (I think). On the very bottom common for both vews is the find panel which will find a word(s) in the cues of the active image depot and select it.
The image itself can be zoomed in/out(buttons), panned (scroll-bars) or fit (the middle button), more tools are planned...

#### Utilities
The third tab of Scripthea contains a utility of converting files from craiyon.com generator (ex Dall-E mini). The images from there have the cue as file name or at least part of it. The Scripthea utility will convert these images in Scripthea image depot.

The description.txt file is a text file where on the left you have the image file name then "=" sing and then the cue. You can edit the file for any reason as you like. 
All the options, external and internal sizes and position are saved on closing and retrieved. 
The current key to access DeepAI API is temporary, if you need more regular access you need to open an account at http://deepai.org and contribute ($2 per 1000 queries). When you get your personal key you need to put it in DeepAI.key file in config folder. (I'm not affiliated to DeepAi in any way.)

#### just run it
If you are interested only in running the Scripthea application: download the whole project, unzip it somewhere and go to ...\Scripthea\bin\Debug\ and run Scripthea.exe from there. If you have concerns about viruses you can compile from the sources - I'm using .NET framework 4.8 and Visual studio 2019 as IDE.

#### contact 
Keep in mind that the application is under active development and don't be too surprised if you see a bug (or two). Let me know at https://sicyon.com/survey/comment.html and I'll do my best to fix it asap. In the same way, you can communicate any ideas for improvement, experiences with the software or your willingness to help me with the project. I would especially appreciate more cue collections preferably organized by subject, see config folder for *.cues files.

#### legal
Scripthea software has been written by and is copyrighted to Teodor Krastev. The sources are distributed under MIT's open source license. 