

console.log('auth.js loaded');

//(function () {
//    const originalFetch = fetch;

//    window.fetch = function (url, options = {}) {
//        const token = localStorage.getItem('jwtToken');

//        if (token) {
//            if (!options.headers) {
//                options.headers = {};
//            }
//            options.headers['Authorization'] = 'Bearer ' + token;
//            console.log('Token added:', options.headers['Authorization']);
//        }
//        else {
//            console.log('No token found in localStorage');
//        }
  
//        return originalFetch(url, options);
//    };
//})();

function getAuthHeaders() {
    const token = localStorage.getItem('jwtToken');
    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
    };
}


