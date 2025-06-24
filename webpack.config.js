const path = require('path');

module.exports = {
    mode: 'development',
    entry: './dist/src/App.js',
    output: {
        path: path.resolve(__dirname, 'public'),
        filename: 'bundle.js'
    },
    devServer: {
        static: {
            directory: path.join(__dirname, 'public'),
        },
        port: 8080,
        open: true
    }
}; 