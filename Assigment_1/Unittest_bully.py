import unittest
from unittest.mock import MagicMock
from unittest.mock import patch
import queue
from Bully_no_opti import election, receive_message, send_message

class TestStringMethods(unittest.TestCase):
    def test_send_message(self):
        # Create test queue with index matching receiving node
        receive_node = 0
        send_node = 1
        test_queue = [queue.Queue(), queue.Queue()]

        # Call function
        send_message("OK", receive_node, send_node, test_queue)

        # Get message and assert
        msg = test_queue[receive_node].get()
        self.assertEqual(msg['type'], "OK")
        self.assertEqual(msg['receiver_id'], 0)
        self.assertEqual(msg['sender_id'], 1)

    def test_receive_message_OK(self):
        # Create test queue with index matching receiving node
        receive_node = 0
        send_node = 1
        max_node = 2
        test_queue = [queue.Queue()] * max_node

        SHARED_THREAD_MSGS = dict()
        SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = True
        SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] = True
        SHARED_THREAD_MSGS['CONTROLLED_BY'] = 0

        # Call functions
        send_message("OK", receive_node, send_node, test_queue)
        receive_message(receive_node, test_queue[receive_node], test_queue, SHARED_THREAD_MSGS)
    
        # Assert that it stops sending election/ being in election mode
        self.assertEqual(SHARED_THREAD_MSGS['ELECTION_ACTIVE'], False) 

    def test_receive_message_COORDINATOR(self):
        # Create test queue with index matching receiving node
        receive_node = 0
        send_node = 1
        max_node = 2
        test_queue = [queue.Queue()] * max_node

        SHARED_THREAD_MSGS = dict()
        SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = False
        SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] = True
        SHARED_THREAD_MSGS['CONTROLLED_BY'] = 0

        # Call functions
        send_message("COORDINATOR", receive_node, send_node, test_queue)
        receive_message(receive_node, test_queue[receive_node], test_queue, SHARED_THREAD_MSGS)
    
        # Assert that it stops sending election/ being in election mode
        self.assertEqual(SHARED_THREAD_MSGS['CONTROLLED_BY'], send_node) 

    @patch("Bully_no_opti.send_message")
    def test_receive_message_ELECTION(self, mock_send_message):
        # Create test queue with index matching receiving node
        send_node_id = 0
        receive_node_id = 1
        max_node = 3
        test_queue = [queue.Queue()] * max_node

        SHARED_THREAD_MSGS = dict()
        SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = False
        SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] = True
        SHARED_THREAD_MSGS['CONTROLLED_BY'] = 0

        # Call function
        send_message("ELECTION", receive_node_id, send_node_id, test_queue)
        receive_message(receive_node_id, test_queue[receive_node_id], test_queue, SHARED_THREAD_MSGS)

        # Get message and assert
        self.assertEqual(SHARED_THREAD_MSGS['ELECTION_ACTIVE'], True)
        self.assertEqual(mock_send_message.call_count, 1)

    @patch("Bully_no_opti.send_message")
    def test_election_id_lower_than_max(self, mock_send_message):
        # Create test queue with index matching receiving node
        node_id = 1
        max_node_id = 5
        test_queue = [queue.Queue()] * max_node_id

        SHARED_THREAD_MSGS = dict()
        SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = False
        SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] = True
        SHARED_THREAD_MSGS['CONTROLLED_BY'] = 0

        # Call function
        election(node_id, max_node_id, test_queue, SHARED_THREAD_MSGS)
    
        # Get message and assert
        self.assertEqual(mock_send_message.call_count, max_node_id-node_id-1)

    @patch("Bully_no_opti.send_message")
    def test_election_id_is_max(self, mock_send_message):
        # Create test queue with index matching receiving node
        node_id = 4
        max_node_id = 5
        test_queue = [queue.Queue()] * max_node_id

        SHARED_THREAD_MSGS = dict()
        SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = False
        SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] = True
        SHARED_THREAD_MSGS['CONTROLLED_BY'] = 0

        # Call function
        election(node_id, max_node_id, test_queue, SHARED_THREAD_MSGS)

        # Get message and assert
        # First assert sends 5 coordinate and 5 terminate = 10!
        self.assertEqual(mock_send_message.call_count, max_node_id*2)

if __name__ == '__main__':
    unittest.main()