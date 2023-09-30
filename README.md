**[Fall 2022 CS 3500] SNAKE by Josie Fiedel & Braden Fiedel**
-------------------------------------------------------------

	SNAKE is a single and multi-player game where players must collect powerup objects
	to increase their score. In single player mode, you as a player must manuever through
	the square-shaped map, collecting powerups and ensuring your survival by avoiding 
	wall obstacles. In multiplayer mode, the difficulty increases with the addition of 
	playing against other snakes with the same goal as you: to achieve the highest score.
	Instead of only having to avoid wall obstacles, you must also choose to avoid or cut off
	the path of other snakes, which resets their scores if they crash into you.

 This project was created with C# .NET Maui in my CS 3500 class with Braden Fiedel to 
 practice the MVC software design pattern, server connections, and multi-threading.

PS8: Client
November 28, 2022
-------------------------------------------------------------


**== How to Use ==**

	When the application is launched, the player is greeted with several buttons and
	textboxes: server, name, connect, help, about, and an empty textbox. 

	The 'server' textbox is where the player must input either a hostname or IP address to
	join an active server. If the player is hosting the server on their own computer, they
	should input "localhost" or "127.0.0.1" in the textbox. Unless another player has the
	information needed to connect to the player's server, this server may be treated as single
	player. The player may also input "snake.eng.utah.edu" to join the CS3500-hosted SNAKE
	server. This server may be treated as a multiplayer game and a student testing ground for PS8.
	
	The 'name' textbox is where the player enters an in-game name that is visible to other
	players. This name must be between 1-16 characters. The default name is "player".

	The 'connect' button is pressed after entering the relevant information into the 'server' 
	and 'name' textboxes. The player cannot connect to the server until they provide these
	inputs. If any error occurs during the connecting process, the player is notified of this
	error and must reconnect. 

	The 'help' button contains information about movement controls. It may be accessed before
	connecting to the game server. The controls are listed below:

		W:  Move forward
		A:  Move left
		S:  Move down
		D:  Move right

	Experimenting with the controls is the best way to learn about their functionality, but
	there are some limits. For example, if a player presses the 'A' key while moving right,
	nothing will happen. This is the case because if the snake runs into itself, it dies.

	The 'about' button acknowledges the contributers to the SNAKE game.

	The empty textbox on the far right of the top of the screen is automatically focused on
	when the game begins, and it may be ignored by the player. The reason for this textbox is 
	so that movement control input is registered depending on which character is entered into 
	the textbox. 


**== Project Notes ==**

		1. Explosion Death Animation

			Creating the explosion death animation was the trickiest and messiest project piece
			to implement. Instead of using a gif, there are 16 image frames of a gif that are
			drawn consecutively to appear as if there is a fire explosion when the snake dies. We
			had the idea of creating a loop to load and draw these images, but we learned that C# 
			does not support dynamic variable creation. Because of this, we had to keep each
			statement separate, which somewhat clutters the code, but it does the trick.

		2. Dynamic Background Color

			One idea that we had was to implement a background that changes color based on the player's
			location in the game world. It took a lot of experimentation, but we settled on using HSVA
			to represent the backdrop color. HSVA changes colors depending on the hue value. With some math,
			we made it so the hue would change depending on the player's distance from the world's origin.
			HSVA also has an opacity option, so we utilized this to add texture to the backdrop with a
			different image. We chose a patterned image to draw behind the HSVA color, which made it so that
			the pattern image is blended with the colors. The same idea is displayed when drawing the Wall and
			Powerup objects. Instead of being the exact same color as the background, the hue is offset by .05,
			which makes the colors slightly different than the background colors. 

			With this concept, there is a slight issue of lag. The snake's movement may appear choppy at times,
			but this is likely due to the math involved behind calculating the color change for each frame. The
			lag does not affect gameplay and is worth it for the dynamic color effect. 

		3. Snake Faces

			To make the Snake objects look more like snakes, we added snake faces which consist of eyes and a tongue.
			We had the difficulty of figuring out how to rotate the image so that a single image may be used for
			multiple directions. We instead decided to focus on more important features and so we resorted to using
			four different images to be drawn on the snake, depending on its orientation.

		4. Snake World Wrapping

			One issue we experienced was figuring out how to avoid drawing a snake's body segment when wrapping around
			the edge of the world. We had to first understand what messages were sent by the server, specifically what
			the body segment coordinates of the snake are when crossing over the snake world. Our implementation of
			drawing snake segments involves iterating through the body vectors and keeping track of the previous vector
			to draw a line between them. We were finally able to properly skip over the wrapped body segment, but there
			is still an issue involving collision detection whenever the snake is wrapped around the edge of the world.
			Luckily, this is a server issue and will be handled in the PS9 server project. 
		
		5. Player Connection & Disconnection
		
			Above the game is a text label that updates when players connect or disconnect from the server. This label
			displays the time of the event, the event type (connect or disconnect), and the name of the player the event
			is referencing. For example, the label will be updated to the string "[11:11 PM] player connected." when a
			player named "player" connects to the server at 11:11 PM. 

PS9: Server
December 8, 2022
------------------------------------------------------------


**== Project Notes ==**
		
		1. Settings.xml
		
			When first starting the project, we struggled with deserializing server information
			from the settings XML file. We did not realize that it was necessary to place the
			to-be-serialized object fields in alphabetical order. We questioned whether other
			settings files would have these fields placed in order too, or if they would differ.
			However, Prof. Martin said that it was expected for other files to be in alphabetical
			order. We also were unsure about where to put the settings file. We first had it in
			our directory in the .Net folder, but this did not transfer over when we were to
			push the project to github. So, we had to place it elsewhere in order for it to be
			submitted to gradescope and also accessible in the program. 

		2. Loop Updating

			We settled on using the 'busy loop' strategy for updating the server's world every frame.
			This method simply involves a stopwatch that keeps track of a specific amount of time in
			a while loop before exiting and performing the update action. Since the SNAKE game does
			not require exact perfect timing, this solution was perfect.

		3. Client Disconnect

			When we were attempting to handle client disconnects, we discovered an error in our
			NetworkController project that we did not previously notice in PS7. In our implementation,
			whenever an error occurred, a new SocketState object would be created and the ID of this
			SocketState would not match our previous one. Because of this, we were not able to get the
			previous SocketState's snake to handle client disconnections properly. So, we decided to
			use the NetworkController.dll from the FullChatSystem and it worked. A new SocketState did
			not appear to be created, so we were able to access our snake of the disconnected client.

		4. Spawn Snake / Powerup

			Since snake and powerup objects must not spawn on top of other game objects, we had a few
			different thoughts about how to handle this requirement. We first thought about representing
			the objects as rectangles, which would make it easier to compare to each other instead of
			just comparing their natural shape. The bounds of these objects in rectangle form is negligible.
			We found this method to be quite messy, so we switched to a different execution method. This
			method involved computing a specific number of points in one object, depending on the size,
			and comparing all of the points' distances to the possibly-colliding object. This method turned
			out to be even messier and less organized than the rectangle overlap solution, so we switched
			back to that method. We managed to make this method utilizable by both powerup and snake objects,
			even though powerups technically have one point, while snake objects have 2 points per segment.
			The way this method works is by calculating the top left and bottom right corner points of both
			objects and using them to determine if there is any overlap. If there is any overlap, then a 
			collision occurs. 

		5. Snake Self-Collisions

			For self-collisions, we used the method that Prof. Martin explained in class. In basic terms, all
			snake segments (pairs of snake vectors) are iterated through and their directions are determined.
			If one of the segments is opposite to the direction of the head, then this is where the collision
			detection starts. The head point is compared to all remaining segments behind this segment, and if
			they happen to overlap with an added offset of the snake's width, then a collision occurs. We solved
			the problem of 180 degree turns by not processing a user's command that would direct their snake into
			their body if they attempted a 180 degree turn. We simply kept track of the distance of one of the
			snake's segments, and once it surpassed the snake's width, then they were allowed to turn.

		6. Powerup Objects

			We only kept a specific number of powerups (the number of MaxPowerups in the settings file) in our 
			world so that were able to reuse them whenever they got hit by a snake. Initially, we started by
			enqueuing the queue until the MaxPowerups number was reached. To keep track of which powerups
			were dead or alive, we placed the dead powerups in a queue that was dequeued whenever a powerup
			needed to be spawned, and enqueued whenever a powerup died. This reuse helped us to not spend
			resources creating new objects whenever a powerup needed to spawn. 
