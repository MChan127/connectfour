Connect4 = {
    // global game variables
    isAuthor: null,
    roomID: null,
    //currentTurn: null,
    activePlayer: null, // currently active player (represented by 0 or 1)
    gameSpaces: null,
    squareSize: null,
    gamePieces: null, // array of all game pieces placed in the game so far
    gamePieceIndex: null, // index of gamePieces so that we can find a particular piece more quickly
                          // given a vertical position
    hubConnection: null,
    gameHub: null, // object for communicating with SignalR hub on the server

    // DOM elements
    $gameStatus: null,
    $gameStatusMsg: null,
    //$turnNumber: null,
    $currentTurnMsg: null,
    activePlayerMsgs: null,
    fg_canvas: null,
    bg_canvas: null,
    fg_ctx: null,
    bg_ctx: null,

    // statuses
    placingPiece: false,
    turnEnded: false,

    // start the connection to the server and the other player
    initGameHub: function() {
        var gameHub = Connect4.gameHub;

        // function called by hub when the opponent finishes making a move
        gameHub.client.endOpponentTurn = function (moveData) {
            switchTurn();
        };

        // function called by hub when the game status changes, such as when
        // a player wins or loses
        // possible values of newStatus:
        // 0 = game start
        // 1 = victory/loss
        // 2 = opponent left the game
        gameHub.client.updateGameStatus = function (newStatus, data) {
            if (data === undefined) {
                data = {};
            }
            
            if (newStatus == 0) {
                Connect4.$gameStatusMsg.text('Game in progress.');
                Connect4.activePlayer = data.first_turn;
                //Connect4.$turnNumber.text(++Connect4.currentTurn);
                //Connect4.$turnNumber.closest('.info').fadeIn(50);

                Connect4.startGame();
                switchTurn(false); // no need to switch turns at the start of the game
                Connect4.$currentTurnMsg.closest('.info').fadeIn(50);
            } else if (newStatus == 1) {

            } else if (newStatus == 2) {

            }
        };

        function joinGame() {
            // join the current room
            return gameHub.server.joinGame(Connect4.roomID);
        };
        function startGame() {
            var deferred = $.Deferred();
            // if the current player is not the author of the room,
            // this means that the game has two participants and it may start
            if (!Connect4.isAuthor)
            {
                console.log("starting game...");
                return gameHub.server.startGame(Connect4.roomID);
            }
            console.log("waiting for opponent...");
            return deferred.resolve();
        }
        Connect4.hubConnection.start().done().then(joinGame).then(startGame).then(function() {
            //Connect4.startGame();
        }).fail(function(error) {
            alert(error);
        });
    },

    // load game variables and draw the game board
    loadGame: function ($, isAuthor, roomID, gameHub, hubConnection) {
        Connect4.isAuthor = isAuthor;
        Connect4.roomID = roomID;
        //Connect4.currentTurn = 0;
        Connect4.gameSpaces = {};
        Connect4.gamePieces = [];
        Connect4.gamePieceIndex = {};
        Connect4.gameHub = gameHub;
        Connect4.hubConnection = hubConnection;

        Connect4.$gameStatus = $('#gameStatus');
        Connect4.$gameStatusMsg = Connect4.$gameStatus.find('.game_status');
        //Connect4.$turnNumber = Connect4.$gameStatus.find('.turn_number');
        Connect4.$currentTurnMsg = Connect4.$gameStatus.find('.current_turn_msg');
        Connect4.activePlayerMsgs = {
            0: Connect4.isAuthor ? "It is now your turn." : "It is the opponent's turn.",
            1: Connect4.isAuthor ? "It is the opponent's turn." : "It is now your turn."
        };
        Connect4.currentTurn = 0;
        Connect4.gamePieces = [];
        Connect4.gamePieceIndex = {};
        
        Connect4.bg_canvas = document.getElementById('gameGrid');
        Connect4.bg_ctx = Connect4.bg_canvas.getContext('2d');

        Connect4.fg_canvas = document.getElementById('gameBoard');
        Connect4.fg_ctx = Connect4.fg_canvas.getContext('2d');

        Connect4.squareSize = 0;

        drawGrid(Connect4.bg_canvas, Connect4.fg_canvas, Connect4.bg_ctx);
        $(window).resize(function () {
            drawGrid(Connect4.bg_canvas, Connect4.fg_canvas, Connect4.bg_ctx);
        });

        function drawGrid(gridElem, boardElem, grid) {
            gridElem.width = $('#content').width() * 0.95;
            boardElem.width = $('#content').width() * 0.95 + 2;
            // calculate the size of a single square so that we can create a
            // grid that's seven squares long, and six squares tall
            Connect4.setSqSize(gridElem.width / 7);
            gridElem.height = Connect4.getSqSize() * 6;
            boardElem.height = Connect4.getSqSize() * 6 + 2;
            // center the game window
            $(gridElem).css('margin-left', ($('#content').width() - $(gridElem).width()) / 2);
            $(boardElem).css('margin-left', ($('#content').width() - $(boardElem).width()) / 2);

            $(boardElem).offset({
                top: $(gridElem).offset().top,
                left: $(gridElem).offset().left
            });

            function drawSquare(x, y, x_index, y_index) {
                grid.strokeStyle = "green";
                grid.strokeWidth = 1;
                grid.strokeRect(x, y, Connect4.getSqSize(), Connect4.getSqSize());

                if (Connect4.gameSpaces[x_index] === undefined || Connect4.gameSpaces[x_index] === null) {
                    Connect4.gameSpaces[x_index] = [];
                }
                Connect4.gameSpaces[x_index][y_index] = {
                    x: x,
                    y: y,
                    x_index: x_index,
                    y_index: y_index
                };
            }
            // draw all the squares on the grid, one by one
            var x_pos = 0;
            var y_pos = 0;
            for (var i = 0; i < 7; i++) {
                for (var j = 0; j < 6; j++) {
                    drawSquare(x_pos, y_pos, i, j);

                    y_pos += Connect4.getSqSize();
                }
                y_pos = 0;
                x_pos += Connect4.getSqSize();
            }
        }

        Connect4.initGameHub();
    },

    startGame: function () {
        var fg_ctx = Connect4.fg_ctx;

        currentMousePos = null;
        highlightedRect = null;
        // get the space on the grid based on the given mouse position
        function getBoxFromMousePos(pointX, pointY) {
            for (var i = 0; i < 7; i++) {
                for (var j = 0; j < 6; j++) {
                    fg_ctx.beginPath();
                    fg_ctx.rect(Connect4.gameSpaces[i][j].x, Connect4.gameSpaces[i][j].y, Connect4.getSqSize(), Connect4.getSqSize());
                    if (fg_ctx.isPointInPath(pointX, pointY)) {
                        return {
                            x: Connect4.gameSpaces[i][j].x,
                            y: Connect4.gameSpaces[i][j].y,
                            x_index: i,
                            y_index: j
                        };
                    }
                }
            }
            return null;
        }
        // check if the player can drop a piece into the given space
        // pieces can only be placed on the floor or on top of another piece
        function canDropPieceHere(box) {
            if (pieceExistsHere(box)) {
                return false;
            }

            if (box.y_index == 5)
                return true;

            return pieceExistsHere(Connect4.gameSpaces[box.x_index][box.y_index + 1]);
        }
        function pieceExistsHere(box) {
            if (Connect4.gamePieceIndex[box.y_index] !== undefined && Connect4.gamePieceIndex[box.y_index] !== null) {
                return Connect4.gamePieceIndex[box.y_index].indexOf(box.x_index) > -1;
            }
            return false;
        }

        // if the mouse leaves the canvas, remove the highlighted square
        Connect4.fg_canvas.onmouseout = function (e) {
            if (!Connect4.playerIsActive()) {
                return;
            }

            if (highlightedRect) {
                var x = highlightedRect.x_index, y = highlightedRect.y_index;
                if (Connect4.gameSpaces[x] === undefined || Connect4.gameSpaces[x] === null || !pieceExistsHere(Connect4.gameSpaces[x][y]))
                    fg_ctx.clearRect(highlightedRect.x, highlightedRect.y, Connect4.getSqSize(), Connect4.getSqSize());
            }
            currentMousePos = null;
        };
        // highlight the spaces on the grid as the mouse moves
        Connect4.fg_canvas.onmousemove = function (e) {
            if (!Connect4.playerIsActive()) {
                return;
            }

            var pointX = e.clientX, pointY = e.clientY;
            if (mouseBox = getBoxFromMousePos(pointX, pointY)) {
                if (currentMousePos === null || currentMousePos.x !== mouseBox.x_index || currentMousePos.y !== mouseBox.y_index) {
                    currentMousePos = {
                        x: mouseBox.x_index,
                        y: mouseBox.y_index
                    };

                    if (highlightedRect) {
                        fg_ctx.clearRect(highlightedRect.x, highlightedRect.y, Connect4.getSqSize(), Connect4.getSqSize());
                    } else {
                        highlightedRect = {};
                    }

                    if (canDropPieceHere(mouseBox)) {
                        fg_ctx.beginPath();
                        fg_ctx.arc(mouseBox.x + (Connect4.getSqSize() / 2), mouseBox.y + (Connect4.getSqSize() / 2), Connect4.getSqSize() / 2.5, 0, Math.PI * 2);
                        fg_ctx.strokeStyle = "blue";
                        fg_ctx.stroke();
                        highlightedRect.x = mouseBox.x;
                        highlightedRect.y = mouseBox.y;
                        highlightedRect.x_index = mouseBox.x_index;
                        highlightedRect.y_index = mouseBox.y_index;
                    }
                }
            }
        };
        // on mouse click, drop a piece into a space if it's allowed
        Connect4.fg_canvas.onclick = function (e) {
            if (!Connect4.playerIsActive()) {
                return;
            }

            var pointX = e.clientX, pointY = e.clientY;

            if (mouseBox = getBoxFromMousePos(pointX, pointY)) {
                // if a piece already exists on this space, return and do nothing
                if (!canDropPieceHere(mouseBox)) {
                    return false;
                }
                Connect4.placingPiece = true;

                if (highlightedRect)
                    fg_ctx.clearRect(highlightedRect.x, highlightedRect.y, Connect4.getSqSize(), Connect4.getSqSize());
                highlightedRect = null;

                Connect4.addPiece(mouseBox.x_index, mouseBox.y_index);

                // remove highlight
                if (highlightedRect) {
                    fg_ctx.clearRect(highlightedRect.x, highlightedRect.y, Connect4.getSqSize(), Connect4.getSqSize());
                }
                fg_ctx.beginPath();
                fg_ctx.arc(mouseBox.x + (Connect4.getSqSize() / 2), (Connect4.getSqSize() / 2) * -1, Connect4.getSqSize() / 2.3, 0, 2 * Math.PI);
                fg_ctx.fillStyle = "blue";
                fg_ctx.fill();
                var totalDistance = (mouseBox.y_index + 1) * Connect4.getSqSize() - Connect4.getSqSize() / 2;
                var currentPos = (Connect4.getSqSize() / 2) * -1;
                var speed = 6;
                (function startAnimation() {
                    var deferred = $.Deferred();

                    var animation = setInterval(function () {
                        // clear the circle first before redrawing in new position
                        fg_ctx.clearRect(mouseBox.x, currentPos - (Connect4.getSqSize() / 2), Connect4.getSqSize(), Connect4.getSqSize());

                        if (currentPos + speed > totalDistance)
                            speed = totalDistance - currentPos;
                        currentPos += speed;

                        fg_ctx.beginPath();
                        fg_ctx.arc(mouseBox.x + (Connect4.getSqSize() / 2), currentPos, Connect4.getSqSize() / 2.3, 0, 2 * Math.PI);
                        fg_ctx.fillStyle = "blue";
                        fg_ctx.fill();

                        if (currentPos >= totalDistance) {
                            clearInterval(animation);
                            deferred.resolve();
                        }
                    }, 0.1);

                    return deferred;
                })().done(function() {
                    setTimeout(function () {
                        Connect4.placingPiece = false;

                        Connect4.switchTurn(true, mouseBox.x_index, mouseBox.y_index);
                    }, 100);
                });
            }
        };
    },

    getSqSize: function () {
        return Connect4.squareSize;
    },
    setSqSize: function (newSize) {
        Connect4.squareSize = newSize;
    },
    addPiece: function (x, y) {
        if (Connect4.gamePieceIndex[y] === undefined || Connect4.gamePieceIndex === null) {
            Connect4.gamePieceIndex[y] = [];
        }
        Connect4.gamePieceIndex[y].push(x);
    },

    playerIsActive: function() {
        return !Connect4.placingPiece && !Connect4.turnEnded;
    },
    switchTurn: function (switchByDefault, x, y) {
        if (switchByDefault === undefined || switchByDefault === true) {
            Connect4.activePlayer = Connect4.activePlayer === 1 ? 0 : 1;
        }

        if (Connect4.isAuthor && Connect4.activePlayer === 1 ||
            !Connect4.isAuthor && Connect4.activePlayer === 0) {
            // end turn
            Connect4.turnEnded = true;
            Connect4.gameHub.server.endTurn(Connect4.roomID, x, y);

        } else {
            // start turn
            Connect4.turnEnded = false;

        }
        Connect4.$currentTurnMsg.text(Connect4.activePlayerMsgs[Connect4.activePlayer]);
    }
};