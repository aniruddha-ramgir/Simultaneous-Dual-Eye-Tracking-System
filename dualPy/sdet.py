	import win32com.client
	import os
	class: StimuliObsever
		incomingQueueinfo = None
		outgoingQueueinfo = None
		incomingQueueinfo.FormatName = None
		outgoingQueueinfo.FormatName = None
		incominQueue = None
		outgoingQueue = None

		def connect():
			try:
				incomingQueueinfo=win32com.client.Dispatch("MSMQ.MSMQQueueInfo")
				outgoingQueueinfo=win32com.client.Dispatch("MSMQ.MSMQQueueInfo")
				omputer_name = os.getenv('COMPUTERNAME')
				incomingQueueinfo.FormatName="direct=os:"+computer_name+"\\PRIVATE$\\SDET-RE"
				outgoingQueueinfo.FormatName="direct=os:"+computer_name+"\\PRIVATE$\\SDET-RQ"
				incominQueue=incomingQueueinfo.Open(2,0)   # Open a ref to Incoming queue
				outgoingQueue=outgoingQueueinfo.Open(2,0)   # Open a ref to outgoing queue
				return True
			except:
				return False
				
		def ready():
			try:
				msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
				msg.Label="REQ"
				msg.Body = "ready"
				msg.Send(outgoingQueue)
				return handleReply("ready")
			except:
				return False
		def start():
			try:
				msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
				msg.Label="REQ"
				msg.Body = "record"
				msg.Send(outgoingQueue)
				return handleReply("record")
			except:
				return False
		def pause():
			try:
				msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
				msg.Label="REQ"
				msg.Body = "pause"
				msg.Send(outgoingQueue)
				return handleReply("pause")
			except:
				return False

		def stop():
			try:
				msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
				msg.Label = "REQ"
				msg.Body  = "stop"
				msg.Send(outgoingQueue)
				return handleReply("stop")
			except:
				return False
			
		def handleReply(str):
			try:
				msg=win32com.client.Dispatch("MSMQ.MSMQMessage")
				msg=outgoingQueue.Receive()
				if(str(msg.Label) != "ACK"): #not ack
					return False;
				if(str(msg.Body) == str) #ack and correlated
					return True
				return False; #neither ack nor correlated
			except:
				return False