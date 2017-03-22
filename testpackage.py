from dualPy import StimuliObserver

print "began"
observer = StimuliObserver()

print "object created"
observer.connect()
print "connected"
#observer.send()
observer.setName("kik")
print "name set"
observer.ready()
print "ready-ed"
observer.start()
print "started"
print "stopped"
observer.stop()