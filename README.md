# Simultaneous-Dual-Eye-Tracking-System
ABOUT:
A C# WPF application that gets Eyetracking data  from 2 different eye trackers parallely. It uses MSMQ to sync the servers and the experimental software.
Documentation to be added once Version 1.0 gets out.

PREQUISITES:
1. Install Python version of the respective version. (Eg: "Python 2.7")
2. Install PyWin32 package of the respective version. (Eg: "pywin32-220.win32-py2.7.exe" )
3. (optional) Install ctypes of the respective version, if you're using Python older than 2.5. (Eg: " ctypes-1.0.2.win32-py2.3.exe" for Python 2.3)

VOCABULARY:
1. "logPath" - location where the logs will be stored. Maintain different logPath for ServerHandlers and ServerHandlerFactory.
2. "HandlerPath" - location of the ServerHandler Executable. It is present in the installation folder itself.
3. "testPath" - default location where the gaze-data is stored. Use it for practice sessions.
4. "mainPath" - location where gaze-data of the actual experiment is stored. Data won't be stored in "mainPath" unless explicitly specified using DualPy functions.
5. "CalibrationBounds" - specifies the dimensions of the calibration runner. 
6. "CalibrationPoints" - specifies the number of the points that will be displayed during calibration.

SETUP:
1. Install the application to a location you're comfortable with. Eg: C:\Program Files (x86)\Simultaneous Dual Eye-Tracking System
2. Go to the installed directory.

3. Look for "ServerHandlerFactory.exe.config" file, and open it using wordpad or Notepad++.
3. 1. Change the value for "HandlerPath" to installation location. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\ServerHandler.exe"
3. 2. Change the value for "logPath" to any location you feel comfortable with. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\logs\\ServerHandlerFactory\\"
3. 3. Do not change anything else in that file. Save and close the file.

4. Look for "ServerHandler.exe.config" file, and open it using wordpad or Notepad++.
4. 1. Change the value for "configPath" to location where the Server config files are stored. Eg: "C:\\config\\"
4. 2. Change the value for "logPath" to any location you feel comfortable with. Eg: Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\logs\\ServerHandler\\"
4. 3. Change the value for "mainPath" to any location you feel comfortable with. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\experiment\\"
4. 4. Change the value for "testPath" to any location you feel comfortable with. Eg: "C:\\Program Files (x86)\\Simultaneous Dual Eye-Tracking System\\test\\"
4. 5. Change the value for "CalibrationBounds" to "0" if you want to calibrate the entire screen. Else, insert an appropriate value.
4. 6. Change the value for "CalibrationPoints" to "9" or other supported numbers. Check the Eye-Tribe doucumentation for supported calibration points.
4. 6. Do not change anything else in this file. Save and close the file.

5. Assuming you're using PsychoPy.Go to the installation directory and look for a package called "DualPy". Then, import it into PsychoPy2. One way to do that is to simply copy the folder to PsychoPy2\Lib.
Example result: C:\Program Files (x86)\PsychoPy2\Lib\site-packages\DualPy

6. In the Python code, 
6. 1. Import the package.
6. 2. Add Code components to the experiment, and add the following wherever appropriate.
6. 2. Create an object of "StimuliObserver" 
6. 3. Call "start() stop() pause() connect()" functions appropriately.


HOW TO RUN:
1. Launch ServerHandlerFactory.exe
2. CLick on "start" and wait for it to load up.
3. Run Calibration on both trackers, one-by-one. Accept the results.
4. Run the experiment in PsychoPy2.
5. Set name of the session.
6. After completing, retrieve the gaze-data from "mainPath" or the "testPath"