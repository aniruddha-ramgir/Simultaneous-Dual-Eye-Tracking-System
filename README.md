# Simultaneous-Dual-Eye-Tracking-System
ABOUT:
A C# WPF application that gets Eyetracking data  from 2 different eye trackers parallely. It uses MSMQ to sync the servers and the experimental software.
Documentation to be added once Version 1.0 gets out.

VOCABULARY:
1. "logPath" - location where the logs will be stored. Maintain different logPath for ServerHandlers and ServerHandlerFactory.
2. "HandlerPath" - location of the ServerHandler Executable. It is present in the installation folder itself.
3. "testPath" - default location where the gaze-data is stored. Use it for practice sessions.
4. "mainPath" - location where gaze-data of the actual experiment is stored. Data won't be stored in "mainPath" unless explicitly specified using DualPy functions.

SETUP:
1. Install the application to a location you're comfortable with. Eg: C:\Program Files (x86)\Simultaneous Dual Eye-Tracking System
2. Go to the installed directory.

3. Look for "ServerHandlerFactory.exe.config" file, and open it using wordpad or Notepad++.
3.1. Change the value for "HandlerPath" to installation location. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\ServerHandler.exe"
3.2. Change the value for "logPath" to any location you feel comfortable with. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\logs\\ServerHandlerFactory\\"
3.3. Do not change anything else in that file. Save and close the file.

4. Look for "ServerHandler.exe.config" file, and open it using wordpad or Notepad++.
4.1 Change the value for "configPath" to location where the Server config files are stored. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\config\\"
4.2 Change the value for "logPath" to any location you feel comfortable with. Eg: Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\logs\\ServerHandler\\"
4.3 Change the value for "mainPath" to any location you feel comfortable with. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\experiment\\"
4.4 Change the value for "testPath" to any location you feel comfortable with. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\test\\"
4.5 Do not change anything else in this file. Save and close the file.

5. Assuming you're using PsychoPy.Go to the installation directory and look for a package called "DualPy". Then, import it into PsychoPy2. One way to do that is to simply copy the folder to PsychoPy2\Lib.
Example result: C:\Program Files (x86)\PsychoPy2\Lib\DualPy

6.In the Python code, 
6.1 Import the package.
6.2 Create an object of "StimuliObserver" 
6.3 Call "start() stop() pause() connect()" functions appropriately.


HOW TO RUN:
1. Launch ServerHandlerFactory.exe
2. CLick on "start" and wait for it to load up.
3. Run Calibration on both trackers, one-by-one. Accept the results.
4. Set name of the session.
4. Run the experiment in PsychoPy2.
5. After completing, retrieve the gaze-data from "mainPath" or the "testPath"