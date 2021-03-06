The `torrent` object
====================

The torrent object represents a torrent in the session and contains various
fields with statistics and data.


Example
-------

.. code-block:: javascript
   :linenos:

   {
     // The torrent info hash.
     "info_hash": "...",

     // The name of the torrent.
     "name": "...",

     // The position of this torrent in the queue. Will be -1 for torrents
     // not in the queue.
     "queue_position": 1,

     // The size of the torrent in bytes.
     "size": 1024,

     // The progress of the current operation.
     "progress": 0.43,

     // The current status of the torrent.
     "status": "downloading",

     // The number of seconds until the torrent finishes its current operation.
     "eta": 100,

     // The current transfer rates (up/down) for the torrrent.
     "dl_rate": 1024,
     "ul_rate": 1024,

     // The number of seeds connected.
     "seeds_connected": 1,

     // The total number of seeds.
     "seeds_total": 12,

     // The number of non-seeds connected.
     "nonseeds_connected": 4,

     // The total number of non-seeds.
     "nonseeds_total": 54
   }
