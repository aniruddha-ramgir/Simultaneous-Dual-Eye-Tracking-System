import win32com.client
import ctypes
import os

class StimuliObserver:
	def connect(self):
		try:
			global incomingQueueinfo
			incomingQueueinfo =win32com.client.Dispatch("MSMQ.MSMQQueueInfo")
			global outgoingQueueinfo
			outgoingQueueinfo=win32com.client.Dispatch("MSMQ.MSMQQueueInfo")
			computer_name = os.getenv('COMPUTERNAME')
			incomingQueueinfo.FormatName="direct=os:"+computer_name+"\\PRIVATE$\\SDET-RE"
			outgoingQueueinfo.FormatName="direct=os:"+computer_name+"\\PRIVATE$\\SDET-RQ"
			global incomingQueue
			incomingQueue=incomingQueueinfo.Open(1,0)   # Open a ref to Incoming queue
			global outgoingQueue
			outgoingQueue=outgoingQueueinfo.Open(2,0)   # Open a ref to outgoing queue
			return True
		except:
			ctypes.windll.user32.MessageBoxW(None, u'Error Connecting to MessageQueue', u'Error: SDET Python package', 0)
			return False
	
	def send(self):
		msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
		msg.Label="ACK"
		msg.Body = '<?xml version="1.0"?><string>'+'kik'+'</string>'
		msg.Send(incomingQueue)
		msg.Send(incomingQueue)
		msg.Send(incomingQueue)
		msg.Send(incomingQueue)
				
	def setName(self,str):
		try:
			msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
			msg.Label="NAME"
			msg.Body = '<?xml version="1.0"?><string>'+str+'</string>'
			msg.Send(outgoingQueue)
			result = self.handleReply(str)
			return result
		except:				
			ctypes.windll.user32.MessageBoxW(None, u'Exception while setting name', u'Error: SDET Python package', 0)
			return False
			
	def isTest(self,int):
		try:
			msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
			msg.Label="TYPE"
			if(int ==1):
				msg.Body = '<?xml version="1.0"?><string>'+'main'+'</string>'
				msg.Send(outgoingQueue)
				return self.handleReply(str)
			else:
				return True;
		except:				
			ctypes.windll.user32.MessageBoxW(None, u'Exception while setting experiment type', u'Error: SDET Python package', 0)
			return False
					
	def ready(self):
		try:
			msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
			msg.Label="REQ"
			msg.Body = '<?xml version="1.0"?><string>'+'ready'+'</string>'
			msg.Send(outgoingQueue)
			return self.handleReply("ready")
		except:				
			ctypes.windll.user32.MessageBoxW(None, u'Exception while sending ready-message', u'Error: SDET Python package', 0)
			return False
	def start(self):
		try:
			msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
			msg.Label="REQ"
			msg.Body = '<?xml version="1.0"?><string>'+'record'+'</string>'
			msg.Send(outgoingQueue)
			return self.handleReply("record")
		except:				
			ctypes.windll.user32.MessageBoxW(None, u'Exception while sending start-message', u'Error: SDET Python package', 0)
			return False
	def pause(self):
		try:
			msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
			msg.Label="REQ"
			msg.Body = '<?xml version="1.0"?><string>'+'pause'+'</string>'
			msg.Send(outgoingQueue)
			return self.handleReply("pause")
		except:					
			ctypes.windll.user32.MessageBoxW(None, u'Exception while sending pause-message', u'Error: SDET Python package', 0)
			return False

	def stop(self):
		try:
			msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
			msg.Label = "REQ"
			msg.Body = '<?xml version="1.0"?><string>'+'stop'+'</string>'
			msg.Send(outgoingQueue)
			return self.handleReply("stop")
		except:					
			ctypes.windll.user32.MessageBoxW(None, u'Exception while sending stop-message', u'Error: SDET Python package', 0)
			return False
		
	def handleReply(self,str):
		try: 
			msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
			msg=incomingQueue.Peek()
			label = unicode(msg.Label)
			body = unicode(msg.Body)
			from xml.etree.ElementTree import fromstring, tostring
			xml = fromstring(body)
			result = tostring(xml).lstrip('<%s>'% xml.tag).rstrip('</%s>' % xml.tag)
			print label
			print body
			print result
			if(label != "ACK"): #not ack
				return False;
			if(result == str): #ack and correlated
				return True
			#ctypes.windll.user32.MessageBoxW(0,u'Reply message unrelated to the sent message.',u'Error: SDET Python package',1)
			return False; #neither ack nor correlated
		except:				
			ctypes.windll.user32.MessageBoxW(None, u'Exception while handling the reply', u'Error: SDET Python package', 0)
			return False