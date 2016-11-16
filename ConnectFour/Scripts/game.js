$(function () {
    (function gameLogic() {
        var c = document.getElementById('gameGrid');
        var fg_ctx = c.getContext('2d');

        var squareSize = 0;
        drawGrid(c, fg_ctx);
        $(window).resize(function () {
            drawGrid(c, fg_ctx);
        });

        function drawGrid(grid, ctx) {
            grid.width = $('#content').width() * 0.95;
            // calculate the size of a single square so that we can create a
            // grid that's seven squares long, and six squares tall
            squareSize = grid.width / 7;
            grid.height = squareSize * 6;
            // center the game window
            $(grid).css('margin-left', ($('#content').width() - $(grid).width()) / 2);

            function drawSquare(x, y) {
                ctx.strokeStyle = "green";
                ctx.strokeWidth = 1;
                ctx.strokeRect(x, y, squareSize, squareSize);
            }
            // draw all the squares on the grid, one by one
            var x_pos = 0;
            var y_pos = 0;
            for (var i = 0; i < 7; i++) {
                for (var j = 0; j < 6; j++) {
                    drawSquare(x_pos, y_pos);

                    y_pos += squareSize;
                }
                y_pos = 0;
                x_pos += squareSize;
            }
        }
    })();
});