# kBlorb
## If you want it done right...

... well, I wouldn't go so far as to say this is doing it right right, but I got a little angry this week at cBlorb and the few other tools available to create blorbs.

cBlorb? Great, uses a single text file to define everything that goes into the output. But it refuses to do `Reso` chunks, which I want to use for reasons. Does it even allow `Rect`? It doesn't seem to.
So I figure I use cBlorb as the first step, then use `blorbtool.py` to manually add in a `Reso` from a file I prepared separately. Which involves weird stuff involving file locks, so my build process has to take the initial `zlb` from cBlorb, let the python script load that in, add the `Reso`, save it to a *new* `zlb`, which is then copied over the now-unlocked initial, and finally the temporary new blorb is removed.

That's nonsense. If there's a better way to do this, I couldn't find it...

So I figured I'd just write my own damn blorb tool.

* Written in C# just because I like it.
* Uses a JSON file as its blurb, because I have my own JSON library and by gawd I'll use it. Working on support for cBlurb-style files though.
* Supports `Reso` and `Rect`.
* Can import images and sounds (png, jpeg, ogg, ~~aif~~, mod, s3m, xm, it)
* Can also import Adrift images and sounds (gif, wav, mid, mp3) if you insist.
* Can designate an image as cover art.
* Automatically skips ahead on indexing if a sound would be indexed < 3.
* Does *not yet* support non Z-Machine/Glulxl game files, and the latter needs testing.
* Has for now very bare-bones IFID support, enough to please WinFrotz and let it show an *About  this game* screen.

An example JSON blurb that I use to develop this with:
```json
{
	//target must be specified for now, may later decide from main game file if missing
	"target": "fowd.zlb",
	"files": [
		//main game must be first in this list.
		"fowd.z5",
		//can use block syntax like this too:
		//{ "type": "exec", "src": "fowd.z5" },
		{
			"type": "picture",
			"index": 1, //can be left out, 
			"src": "title.png",

			"ratios": [ 1, 0, 0 ] //allow scaling, but don't care by how much
			//defaults to all zeroes (don't scale)
			//three numbers is shorthand, expands to six:
			//[ 1, 1, 0, 1, 0, 1 ]
			//so 1/1, 0/1, 0/1
			//single number is also allowed:
			//[ 2 ] --> [ 2, 2, 2 ] --> [ 2,1, 2,1, 2,1 ]
			//all that is only used if there's a resolution block
		},
		//alt text, also for sound resources.
		{ "src": "landscape.png", "alt": "A beautiful landscape." },
		{ "src": "cover.jpg", "cover": true }
		//last pic to say it's the cover wins

		//placeholder rect support:
		{ "type": "rect", "size": [ 32, 32 ] },
		{ "index": 3, "src": "sun_sex_girls.mod" }
		//shorthand rects are just a two number array:
		[ 32, 32 ],
		//
		"sun_sex_girls.mod",
		"another.png"
	],
	//optional, causes a Reso chunk to appear with ratio info for all pics.
	"resolution": {
		"standard": [320, 200],
		//min max may be left out, will default to match standard.
		"minimum": [320, 200],
		"maximum": [320, 200]
	},
	"meta": {
		//**work in progress**
		//id is generated on the fly if missing.
		"id": "A7B0CE51-AF32-4134-9C76-CCE0848F5A1B",
		"title": "Farrah's Ordinary Work Day",
		"headline": "A Demo Drabble",
		"author": "Kawa",
		"description": "Mew?"
	}
	//you can also do "meta": "gameinfo.xml" if you want,
	//or an embedded XML document if you're a nutcase.
}
```

I tested all this by comparing the output from kBlorb to that of my cBlorb/blorbtool mashup. As it is now, it fits my personal needs just fine. Hell, I didn’t even *want* an IFID

**This is not yet fit for release** so there’s nothing in the Releases page or whatever *yet*.
