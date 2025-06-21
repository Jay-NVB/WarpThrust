# WarpThrust

Better PersistentThrust plugin, now with built-in configs for Far Future Technologies and Near Future Propulsion! <br>
RO compatible, RO configs for Near Future and Far Future engines are also included courtesy of Tyler Raiz.<br>
Also includes configs for the stock NERV and Dawn engines.

REQUIRES: ModuleManager, Persistent Rotation

Instructions:
- Power your craft with a plugin-compatible engine (it will have a WarpThrust box in the right-click window if it is).
- When in flight, select the desired SAS orientation. DO NOT USE MECHJEB, the plugin will fail to activate.
- Engage on-rails timewarp and watch the magic happen.

Standalone Realism Overhaul configurations for Far Future and Near Future engines created by Tyler Raiz are available here:
- [FFT](https://www.youtube.com/watch?v=cx9pL2oRzeA)
- [NFP](https://www.youtube.com/watch?v=9CWHvZVvwSk)

![image](https://github.com/user-attachments/assets/9d11ae6a-9de9-4cc0-9f5b-2d285807ceb1)

# How to configure engines yourself
- Create a .cfg file in GameData, and name it "<engine>WarpThrustPatch"
- In that .cfg file, create a ModuleManager patch that adds the following module to the engine you want to configure:
  
	MODULE<br>
	{<br>
		name = WarpThrust<br>
	}
