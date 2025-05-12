### Scripthea

#### Intro
Scripthea is a free, open-source Windows application designed to streamline the process of crafting prompts for text-to-image AI generators like Stable Diffusion. Developed by Teodor Krastev, Scripthea offers a structured environment for building, testing, and refining prompts, making it an invaluable tool for artists, designers, and AI enthusiasts seeking greater control over their creative outputs. At its core, Scripthea simplifies prompt engineering by breaking down prompts into two components: cues (descriptive text or phrase) and modifiers (attributes like style, lighting, or artist references). This modular approach allows users to experiment with various combinations, facilitating a more systematic exploration of visual styles and themes. A short introduction video is in https://youtu.be/BSUsCHmuI0Y .

After composing your prompt, you can use API access to Stable Diffusion text-to-image generator, and here is the strong side of Scripthea. The prompt and the picture become part of your collection (image depot) with a convenient image viewer on the second tab. On top of that, you can "scan". After selecting some texts and some modifiers, Scan will combine them as every text will be combined with every modifier. So for example if you would like to see how a particular painter would paint different subjects or how specific topic would be painted by different painters. Scripthea will generate all the combinations for you (scan) and query the active API for you to put them in an image collection.
Alternatively, you can copy and paste it into your favourite text-to-image generator and see to result in your browser. 

### Key features

#### Here is the prompt composer tab...
![Scripthea-1.png](/docs/Scripthea-1.jpg)

Craft prompts using a combination of cues and modifiers. Operate in two modes: in Single mode, you can use one cue and more than one modifier; in Scan mode, you can select any number of cues and any number of modifiers, but each prompt will combine only one cue with one modifier. Modifiers are divided into categories and to use modifiers from any category, you need to check the category itself. If you wonder about any modifier, hover over it, and there will be a hint for some of them. If you right-click on any modifier, you will be asked to confirm a Google search for that modifier. In options, you can specify the image depot folder where the images from your scan (or single query) will go.

#### ...and here is the image depot viewer
![grid-view.png](/docs/viewer-grid.jpg)

The viewer shows a Scripthea image depot (a folder with a bunch of images and decription.idf file for the prompts). Organize and review generated images alongside their corresponding prompts. The viewer supports both grid and table views, aiding in visual analysis and comparison. After reviewing the images, you can rate them for later categorization.

#### Image depot utilities
The third tab is Image Depot Master as Image Depot manager - copy, move, delete images, check for consistency, etc 

The fourth tab of Scripthea contains a utility for importing images generated elsewhere. The export utility images will export a selected subset of your image depot to a folder or create an image webpage for publishing.

#### Python scripting
The last tab offers you automation of your workflow by using integrated Python scripting with access to key Scripthea's features.

### Why choose Scripthea?

Scripthea stands out by offering a structured approach to prompt engineering, enabling users to: 
- Systematically explore various artistic styles and themes
- Efficiently manage and review large batches of generated images.
- Gain deeper insights into the relationship between prompts and visual outputs.
Whether you're a seasoned AI artist or a newcomer eager to delve into text-to-image generation, Scripthea provides the tools and structure to elevate your creative process.

#### contact 
Keep in mind that the application is under active development, so let me know about any bug at <scripthea(at)sicyon.com> and I'll do my best to fix it asap. In the same way, you can communicate any ideas for improvement, experiences with the software or your willingness to help me with the project. If you have any concerns about viruses, compile from the sources - I'm using C#, .NET framework 4.8 with WPF and Visual Studio 2019 as IDE.

#### legal
Scripthea software has been written by and is copyrighted to Teodor Krastev. The sources are distributed under MIT's open source license. 
