import random, sys, time, queue
from threading import Thread

def run_node(node_id, starter, max_node_id, SHARED_QUEUE_INFO):
    print("Node " + str(node_id) + " started")
    receive_queue = SHARED_QUEUE_INFO[node_id]

    SHARED_THREAD_MSGS = dict()
    SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = False
    SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] = False
    SHARED_THREAD_MSGS['CONTROLLED_BY'] = 0

    proc = Thread(target=receive_message, args=(node_id, receive_queue, SHARED_QUEUE_INFO, SHARED_THREAD_MSGS))
    proc.start()

    # Start the algo if node is selected as starter
    if starter:
        SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = True

    while 1:
        if SHARED_THREAD_MSGS['ELECTION_ACTIVE'] == True:
            election(node_id, max_node_id,
                 SHARED_QUEUE_INFO, SHARED_THREAD_MSGS)

        if SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] == True:
            print("Node id: " + str(node_id) + " controlled by " + str(SHARED_THREAD_MSGS['CONTROLLED_BY']))
            break
    
    proc.join()

def election(node_id, max_node_id, send_queue, SHARED_THREAD_MSGS):
    drop_out_flag = True

    if node_id == max_node_id-1:
        drop_out_flag = False
    else:
        print("Node id: " + str(node_id) + " sent election msg to node id " +
              str(node_id+1) + " to " + str(max_node_id-1))
        for i in range(node_id+1, max_node_id):
            send_message("ELECTION", str(i), node_id, send_queue)
        
    if drop_out_flag == False:
        for i in range(max_node_id):
            send_message("COORDINATOR", str(i), node_id, send_queue)
            send_message("TERMINATE", str(i), node_id, send_queue)

    SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = False

def receive_message(node_id, receiver_queue, send_queue, SHARED_THREAD_MSGS):
    while 1:
        try:
            msg = receiver_queue.get(timeout=0.1)

            if(msg['type'] == "ELECTION"):
                send_message("OK", msg['sender_id'], node_id, send_queue)
                SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = True

            elif(msg['type'] == "OK"):
                print("Node id: " + str(node_id) +
                      " received OK from node id: ", msg['sender_id'])
                SHARED_THREAD_MSGS['ELECTION_ACTIVE'] = False

            elif(msg['type'] == "COORDINATOR"):
                SHARED_THREAD_MSGS['CONTROLLED_BY'] = int(msg['sender_id'])

            elif(msg['type'] == "TERMINATE"):
                SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] = True
        except:
            pass
        if SHARED_THREAD_MSGS['TERMINATE_MESSAGE'] == True:
            break


def send_message(msg_type, receive_node, send_node, send_queue):
    queue = send_queue[int(receive_node)]
    resp_msg = {"sender_id": send_node,
                "receiver_id": receive_node, "type": msg_type}
    queue.put(resp_msg)

if __name__ == '__main__':
    quests = sys.argv

    if(len(quests) == 3):
        num_nodes = int(sys.argv[1])
        num_alive_nodes = int(sys.argv[2])
        alive_nodes = random.sample(range(0,num_nodes),num_alive_nodes)
        starter_node = [0]# random.sample(alive_nodes, 1)
        processes = []

        SHARED_QUEUE_INFO = [None] * num_nodes
        for k in range(num_nodes):
            SHARED_QUEUE_INFO[k] = queue.Queue()

        print("Number of nodes: " + str(num_nodes))
        print("Alive nodes: " + str(alive_nodes))
        print("Start node choosen: " + str(starter_node) + "\n")

        # Start processes
        for node_id in alive_nodes:
            if(node_id == starter_node[0]):
                starter = True
            else:
                starter = False
            proc = Thread(target=run_node, args=(
                node_id, starter, num_nodes, SHARED_QUEUE_INFO))
            processes.append(proc)

        starttime = time.time()

        # Start alive node processes
        for ap in processes:
            ap.start()

        # Join alive node processes
        for ap in processes:
            ap.join()

        endtime = time.time()

    print("Done - Time elapsed " + str(endtime-starttime))