let token = localStorage.getItem('token') || '';
let role = localStorage.getItem('role') || '';
let username = localStorage.getItem('username') || '';
let voiceTranscript = '';
let menuItems = [];
const apiBaseUrl = 'http://localhost:5010/api';
const aiVoiceUrl = 'http://localhost:5000/parse_order';
const aiFeedbackUrl = 'http://localhost:5000/analyze_feedback';

// Polling for order updates
let pollingInterval = null;

function setError(message, totalPrice = null, isSuccess = false) {
    const errorElement = document.getElementById('error');
    const successElement = document.getElementById('success');
    if (!errorElement || !successElement) {
        console.error('Error or success element not found');
        return;
    }
    const formattedTotal = totalPrice && !isNaN(totalPrice) ? `$${totalPrice.toFixed(2)}` : '';
    const finalMessage = totalPrice ? `${message} Total: ${formattedTotal}` : message;
    if (isSuccess) {
        errorElement.style.display = 'none';
        successElement.style.display = 'block';
        successElement.innerText = finalMessage;
    } else {
        errorElement.style.display = 'block';
        successElement.style.display = 'none';
        errorElement.innerText = finalMessage;
    }
}

function parseErrorResponse(error, errorText) {
    try {
        const errorObj = JSON.parse(errorText || '{}');
        if (errorObj.errors) {
            return Object.entries(errorObj.errors)
                .map(([key, value]) => `${key}: ${value.join(', ')}`)
                .join('; ');
        }
        return errorObj.error || errorObj.message || error.message || 'Unknown error';
    } catch {
        return errorText || error.message || 'Unknown error';
    }
}

function validateLocalStorage() {
    console.log('Validating LocalStorage');
    try {
        localStorage.setItem('test', 'test');
        localStorage.removeItem('test');
        console.log('LocalStorage is accessible');
        return true;
    } catch (err) {
        console.error('LocalStorage access failed:', err.message, err.stack);
        setError('Error: LocalStorage is not accessible. Check browser settings.');
        return false;
    }
}

function showModule(moduleId) {
    console.log('Showing module:', moduleId);
    document.querySelectorAll('.module').forEach(module => module.classList.remove('active'));
    document.getElementById(moduleId).classList.add('active');
    setError('');
    if (moduleId === 'menu') fetchMenu();
    if (moduleId === 'order') {
        fetchOrders();
        fetchMenuItemsForOrder();
        startOrderPolling();
    } else {
        stopOrderPolling();
    }
    if (moduleId === 'feedback') fetchFeedback();
    if (moduleId === 'report') fetchReport();
}

function startOrderPolling() {
    if (pollingInterval) return;
    pollingInterval = setInterval(fetchOrders, 5000); // Poll every 5 seconds
    console.log('Started order polling');
}

function stopOrderPolling() {
    if (pollingInterval) {
        clearInterval(pollingInterval);
        pollingInterval = null;
        console.log('Stopped order polling');
    }
}

function updateUI() {
    const authDiv = document.getElementById('auth');
    const dashboardDiv = document.getElementById('dashboard');
    const userInfo = document.getElementById('user-info');
    const logoutBtn = document.getElementById('logout');
    const reportBtn = document.getElementById('report-btn');
    const adminMenuForm = document.getElementById('admin-menu-form');
    const customerOrderForm = document.getElementById('customer-order-form');
    const callOrderForm = document.getElementById('call-order-form');
    const customerFeedbackForm = document.getElementById('customer-feedback-form');

    if (token) {
        authDiv.style.display = 'none';
        dashboardDiv.style.display = 'block';
        userInfo.innerText = `Welcome, ${username} (${role})`;
        logoutBtn.style.display = 'block';
        reportBtn.style.display = role === 'Admin' ? 'block' : 'none';
        adminMenuForm.style.display = role === 'Admin' ? 'block' : 'none';
        customerOrderForm.style.display = role === 'Customer' ? 'block' : 'none';
        callOrderForm.style.display = (role === 'Admin' || role === 'Kitchen') ? 'block' : 'none';
        customerFeedbackForm.style.display = role === 'Customer' ? 'block' : 'none';
        showModule('menu');
    } else {
        authDiv.style.display = 'block';
        dashboardDiv.style.display = 'none';
        userInfo.innerText = '';
        logoutBtn.style.display = 'none';
        stopOrderPolling();
    }
}

document.getElementById('auth-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const isLogin = document.getElementById('auth-title').innerText === 'Login';
    const usernameInput = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value.trim();
    const roleInput = document.getElementById('role').value;
    const url = isLogin ? `${apiBaseUrl}/Auth/login` : `${apiBaseUrl}/Auth/register`;
    const body = isLogin ? { username: usernameInput, password } : { username: usernameInput, password, role: roleInput };

    try {
        console.log(`Attempting ${isLogin ? 'login' : 'register'} for:`, usernameInput);
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        console.log(`Auth API Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Auth API Raw Response: ${responseText || 'Empty response'}`);
        let data = {};
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Auth Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse auth response:', jsonError.message, 'Raw Response:', responseText);
                throw new Error(`Auth failed: Invalid JSON response - ${responseText || 'Empty'}`);
            }
        }
        if (!response.ok) {
            throw new Error(`Auth failed: ${response.status} - ${data.error || responseText || 'No error details'}`);
        }
        if (isLogin) {
            if (data.token) {
                token = data.token;
                const payload = JSON.parse(atob(data.token.split('.')[1]));
                role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role || '';
                if (!role) throw new Error('Role not found in token payload');
                username = usernameInput;
                localStorage.setItem('token', token);
                localStorage.setItem('role', role);
                localStorage.setItem('username', username);
                console.log('Stored in localStorage:', { token: token.slice(0, 10) + '...', role, username });
                setError('Login successful.', null, true);
                setTimeout(() => updateUI(), 500);
            } else {
                throw new Error('Login failed: No token received.');
            }
        } else {
            setError('Registration successful. Please login.', null, true);
            document.getElementById('auth-title').innerText = 'Login';
            document.getElementById('role-label').style.display = 'none';
            document.getElementById('role').style.display = 'none';
            document.getElementById('toggle-auth').innerText = 'Need an account? Register';
            document.getElementById('auth-form').reset();
        }
    } catch (err) {
        console.error('Auth Error:', err.message, err.stack);
        setError(`Error: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
});

document.getElementById('toggle-auth').addEventListener('click', () => {
    const isLogin = document.getElementById('auth-title').innerText === 'Login';
    document.getElementById('auth-title').innerText = isLogin ? 'Register' : 'Login';
    document.getElementById('role-label').style.display = isLogin ? 'block' : 'none';
    document.getElementById('role').style.display = isLogin ? 'block' : 'none';
    document.getElementById('toggle-auth').innerText = isLogin ? 'Already have an account? Login' : 'Need an account? Register';
    setError('');
    document.getElementById('auth-form').reset();
});

document.getElementById('logout').addEventListener('click', () => {
    console.log('Logging out');
    token = '';
    role = '';
    username = '';
    voiceTranscript = '';
    localStorage.clear();
    console.log('LocalStorage after logout:', {
        token: localStorage.getItem('token') || 'empty',
        role: localStorage.getItem('role') || 'empty',
        username: localStorage.getItem('username') || 'empty'
    });
    setError('Logged out successfully.', null, true);
    setTimeout(() => updateUI(), 500);
});

async function fetchMenu() {
    try {
        console.log('Fetching menu items with token:', token ? token.slice(0, 10) + '...' : 'empty');
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Menu`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log(`Menu API Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Menu API Raw Response: ${responseText || 'Empty response'}`);
        if (response.status === 401) {
            setError('Authentication failed: Please log in again.');
            localStorage.clear();
            updateUI();
            return;
        }
        let data = [];
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Menu Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse menu response:', jsonError.message, 'Raw Response:', responseText);
                throw new Error(`Fetch menu failed: Invalid JSON response - ${responseText || 'Empty'}`);
            }
        }
        if (!response.ok) {
            throw new Error(`Fetch menu failed: ${response.status} - ${data.error || responseText || 'No error details'}`);
        }
        menuItems = data.map(item => ({
            id: item.id || item.Id,
            name: item.name || item.Name,
            price: typeof item.price === 'number' ? item.price : (typeof item.Price === 'number' ? item.Price : null)
        })).filter(item => {
            if (!item.id || !item.name || item.price == null || isNaN(item.price)) {
                console.warn(`Invalid menu item: ${JSON.stringify(item)}`);
                return false;
            }
            return true;
        });
        console.log('Filtered Menu Items:', JSON.stringify(menuItems, null, 2));
        const menuList = document.getElementById('menu-list');
        if (!menuList) {
            console.error('Menu list element not found');
            return;
        }
        menuList.innerHTML = menuItems.length
            ? menuItems.map(item => `<tr><td>${item.name}</td><td>$${item.price.toFixed(2)}</td></tr>`).join('')
            : '<tr><td colspan="2">No menu items available.</td></tr>';
        setError('');
    } catch (err) {
        console.error('Menu Fetch Error:', err.message, err.stack);
        setError(`Error: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
}

async function fetchMenuItemsForOrder() {
    try {
        console.log('Fetching menu items for order with token:', token ? token.slice(0, 10) + '...' : 'empty');
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Menu`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log(`Menu Fetch Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Menu Fetch API Raw Response: ${responseText || 'Empty response'}`);
        if (response.status === 401) {
            setError('Authentication failed: Please log in again.');
            localStorage.clear();
            updateUI();
            return;
        }
        let data = [];
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Menu Fetch Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse menu fetch response:', jsonError.message, 'Raw Response:', responseText);
                throw new Error(`Fetch menu failed: Invalid JSON response - ${responseText || 'Empty'}`);
            }
        }
        if (!response.ok) {
            throw new Error(`Fetch menu failed: ${response.status} - ${data.error || responseText || 'No error details'}`);
        }
        menuItems = data.map(item => ({
            id: item.id || item.Id,
            name: item.name || item.Name,
            price: typeof item.price === 'number' ? item.price : (typeof item.Price === 'number' ? item.Price : null)
        })).filter(item => {
            if (!item.id || !item.name || item.price == null || isNaN(item.price)) {
                console.warn(`Invalid menu item: ${JSON.stringify(item)}`);
                return false;
            }
            return true;
        });
        console.log('Filtered Menu Items:', JSON.stringify(menuItems, null, 2));
        const orderSelect = document.getElementById('order-items');
        const callOrderSelect = document.getElementById('call-order-items');
        if (!orderSelect || !callOrderSelect) {
            console.error('Order select elements not found');
            return;
        }
        orderSelect.innerHTML = callOrderSelect.innerHTML = menuItems.length
            ? menuItems.map(item => `<option value="${item.id}">${item.name} - $${item.price.toFixed(2)}</option>`).join('')
            : '<option>No items available</option>';
        const quantityControls = document.getElementById('quantity-controls');
        const callQuantityControls = document.getElementById('call-quantity-controls');
        if (!quantityControls || !callQuantityControls) {
            console.error('Quantity controls elements not found');
            return;
        }
        quantityControls.innerHTML = callQuantityControls.innerHTML = menuItems.length
            ? menuItems.map(item => `
                <div>
                    <label for="quantity-${item.id}">${item.name} Quantity:</label>
                    <input type="number" id="quantity-${item.id}" class="quantity-input" value="1" min="1">
                </div>
            `).join('')
            : '';
        if (!menuItems.length) {
            setError('No valid menu items available. Please add items in Menu Management.');
        } else {
            setError('');
        }
    } catch (err) {
        console.error('Menu Fetch Error:', err.message, err.stack);
        setError(`Error: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
}

async function fetchOrders() {
    try {
        console.log('Fetching orders with token:', token ? token.slice(0, 10) + '...' : 'empty');
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Order`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log(`Orders API Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Orders API Raw Response: ${responseText || 'Empty response'}`);
        if (response.status === 401) {
            setError('Authentication failed: Please log in again.');
            localStorage.clear();
            updateUI();
            return;
        }
        let data = [];
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Orders Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse orders response:', jsonError.message, 'Raw Response:', responseText);
                throw new Error(`Fetch orders failed: Invalid JSON response - ${responseText || 'Empty'}`);
            }
        }
        if (!response.ok) {
            throw new Error(`Fetch orders failed: ${response.status} - ${data.error || responseText || 'No error details'}`);
        }
        const orderList = document.getElementById('order-list');
        if (!orderList) {
            console.error('Order list element not found');
            setError('Order list element not found.');
            return;
        }
        orderList.innerHTML = data.length
            ? data.map(o => {
                const totalPrice = typeof o.totalPrice === 'number' && !isNaN(o.totalPrice) ? o.totalPrice.toFixed(2) : '0.00';
                const customerName = o.customerName || 'Unknown';
                const orderDate = o.orderDate ? new Date(o.orderDate).toLocaleString() : 'N/A';
                const items = Array.isArray(o.items) && o.items.length
                    ? o.items.map(item => `${item.quantity || 1} x ${item.name || 'Unknown'} ($${typeof item.price === 'number' ? item.price.toFixed(2) : '0.00'})`).join(', ')
                    : 'No items';
                const status = o.status || 'Unknown';
                const statusOptions = ['Received', 'In Kitchen', 'Ready', 'Delivered']
                    .map(s => `<option value="${s}" ${status === s ? 'selected' : ''}>${s}</option>`)
                    .join('');
                const statusCell = (role === 'Admin' || role === 'Kitchen')
                    ? `<select class="status-select" data-order-id="${o.id}" onchange="updateOrderStatus(${o.id}, this.value)">
                        ${statusOptions}
                    </select>`
                    : `<span>${status}</span>`;
                return `
                    <tr>
                        <td>${o.id || 'N/A'}</td>
                        <td>${customerName}</td>
                        <td>${orderDate}</td>
                        <td>${items}</td>
                        <td>$${totalPrice}</td>
                        <td>${statusCell}</td>
                    </tr>`;
            }).join('')
            : '<tr><td colspan="6">No orders available.</td></tr>';
        setError('');
    } catch (err) {
        console.error('Orders Fetch Error:', err.message, err.stack);
        setError(`Error fetching orders: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
}

async function updateOrderStatus(orderId, status) {
    try {
        console.log(`Updating status for order ${orderId} to ${status}`);
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        if (role !== 'Admin' && role !== 'Kitchen') {
            setError('Only Admin or Kitchen can update order status.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Order/${orderId}/status`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ status })
        });
        console.log(`Update Status Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Update Status API Raw Response: ${responseText || 'Empty response'}`);
        if (response.status === 401) {
            setError('Authentication failed: Please log in again.');
            localStorage.clear();
            updateUI();
            return;
        }
        let data = {};
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Update Status Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse status update response:', jsonError.message, 'Raw Response:', responseText);
                throw new Error(`Update status failed: Invalid JSON response - ${responseText || 'Empty'}`);
            }
        }
        if (!response.ok) {
            throw new Error(`Update status failed: ${response.status} - ${data.error || responseText || 'No error details'}`);
        }
        setError(`Order ${orderId} status updated to ${status}.`, null, true);
        await fetchOrders();
    } catch (err) {
        console.error('Update Status Error:', err.message, err.stack);
        setError(`Error updating status: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
}

async function placeOrder() {
    const customerName = document.getElementById('order-customer-name')?.value.trim();
    const customerPhone = document.getElementById('order-customer-phone')?.value.trim();
    const orderItemsSelect = document.getElementById('order-items');
    if (!customerName || !customerPhone || !orderItemsSelect) {
        console.error('Missing required fields:', { customerName, customerPhone, orderItemsSelect });
        setError('Missing required fields: customer name, phone, or items.');
        return;
    }
    if (!token) {
        console.error('No token available for order placement');
        setError('No token available. Please login again.');
        return;
    }
    console.log('Menu Items:', JSON.stringify(menuItems, null, 2));
    const selectedItems = Array.from(orderItemsSelect.selectedOptions)
        .map(option => {
            console.log(`Processing option: ${option.value}, text: ${option.text}`);
            const item = menuItems.find(i => i.id === parseInt(option.value));
            if (!item) {
                console.warn(`Menu item not found for ID: ${option.value}`);
                return null;
            }
            if (typeof item.price !== 'number' || isNaN(item.price) || item.price <= 0) {
                console.warn(`Invalid price for item ID: ${option.value}, item: ${JSON.stringify(item)}`);
                return null;
            }
            const quantityInput = document.getElementById(`quantity-${option.value}`);
            const quantity = quantityInput ? parseInt(quantityInput.value) || 1 : 1;
            return {
                menuItemId: parseInt(option.value),
                name: item.name || 'Unknown',
                quantity: quantity,
                price: item.price
            };
        })
        .filter(item => item !== null);

    console.log('Place Order Inputs:', { customerName, customerPhone, selectedItems });
    if (!selectedItems.length) {
        console.error('No valid items selected');
        setError('Please select at least one item.');
        return;
    }

    const totalPrice = selectedItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    if (isNaN(totalPrice)) {
        console.error('Total price calculation failed:', selectedItems);
        setError('Error calculating total price.');
        return;
    }

    const order = {
        order: {
            customerName,
            customerPhone,
            totalPrice,
            status: 'Received',
            orderDate: new Date().toISOString().split('.')[0] + 'Z',
            items: selectedItems
        }
    };
    console.log('Place Order Payload:', JSON.stringify(order, null, 2));

    try {
        const response = await fetch(`${apiBaseUrl}/Order`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(order)
        });
        console.log(`Place Order Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Place Order API Raw Response: ${responseText || 'Empty response'}`);
        let data = {};
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Place Order Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse place order response:', jsonError.message, 'Raw Response:', responseText);
                setError(`Order placement failed: Invalid server response. Please try again.`, null, false);
                return;
            }
        }
        if (!response.ok) {
            let errorMessage = data.error || responseText || 'No error details';
            if (data.errors) {
                errorMessage = Object.entries(data.errors)
                    .map(([key, value]) => `${key}: ${value.join(', ')}`)
                    .join('; ');
            }
            throw new Error(`Place order failed: ${response.status} - ${errorMessage}`);
        }
        console.log('Order placed successfully, resetting form');
        document.getElementById('order-customer-name').value = '';
        document.getElementById('order-customer-phone').value = '';
        orderItemsSelect.selectedIndex = -1;
        document.querySelectorAll('.quantity-input').forEach(input => input.value = '1');
        setError(`Order placed successfully. Order ID: ${data.orderId}`, data.totalPrice, true);
        await fetchOrders();
    } catch (err) {
        console.error('Place Order Error:', err.message, err.stack);
        setError(`Error placing order: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
}

async function placeCallOrder() {
    const customerName = document.getElementById('call-customer-name')?.value.trim();
    const customerPhone = document.getElementById('call-customer-phone')?.value.trim();
    const orderItemsSelect = document.getElementById('call-order-items');
    if (!customerName || !customerPhone || !orderItemsSelect) {
        console.error('Missing required fields:', { customerName, customerPhone, orderItemsSelect });
        setError('Missing required fields: customer name, phone, or items.');
        return;
    }
    if (!token) {
        console.error('No token available for call order placement');
        setError('No token available. Please login again.');
        return;
    }
    console.log('Menu Items:', JSON.stringify(menuItems, null, 2));
    const selectedItems = Array.from(orderItemsSelect.selectedOptions)
        .map(option => {
            console.log(`Processing option: ${option.value}, text: ${option.text}`);
            const item = menuItems.find(i => i.id === parseInt(option.value));
            if (!item) {
                console.warn(`Menu item not found for ID: ${option.value}`);
                return null;
            }
            if (typeof item.price !== 'number' || isNaN(item.price) || item.price <= 0) {
                console.warn(`Invalid price for item ID: ${option.value}, item: ${JSON.stringify(item)}`);
                return null;
            }
            const quantityInput = document.getElementById(`quantity-${option.value}`);
            const quantity = quantityInput ? parseInt(quantityInput.value) || 1 : 1;
            return {
                menuItemId: parseInt(option.value),
                name: item.name || 'Unknown',
                quantity: quantity,
                price: item.price
            };
        })
        .filter(item => item !== null);

    console.log('Place Call Order Inputs:', { customerName, customerPhone, selectedItems });
    if (!selectedItems.length) {
        console.error('No valid items selected for call order');
        setError('Please select at least one item.');
        return;
    }

    const totalPrice = selectedItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    if (isNaN(totalPrice)) {
        console.error('Total price calculation failed:', selectedItems);
        setError('Error calculating total price.');
        return;
    }

    const order = {
        order: {
            customerName,
            customerPhone,
            totalPrice,
            status: 'Received',
            orderDate: new Date().toISOString().split('.')[0] + 'Z',
            items: selectedItems
        }
    };
    console.log('Place Call Order Payload:', JSON.stringify(order, null, 2));

    try {
        const response = await fetch(`${apiBaseUrl}/Order`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(order)
        });
        console.log(`Place Call Order Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Place Call Order API Raw Response: ${responseText || 'Empty response'}`);
        let data = {};
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Place Call Order Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse place call order response:', jsonError.message, 'Raw Response:', responseText);
                setError(`Call order placement failed: Invalid server response. Please try again.`, null, false);
                return;
            }
        }
        if (!response.ok) {
            let errorMessage = data.error || responseText || 'No error details';
            if (data.errors) {
                errorMessage = Object.entries(data.errors)
                    .map(([key, value]) => `${key}: ${value.join(', ')}`)
                    .join('; ');
            }
            throw new Error(`Place call order failed: ${response.status} - ${errorMessage}`);
        }
        console.log('Call order placed successfully, resetting form');
        document.getElementById('call-customer-name').value = '';
        document.getElementById('call-customer-phone').value = '';
        orderItemsSelect.selectedIndex = -1;
        document.querySelectorAll('.quantity-input').forEach(input => input.value = '1');
        setError(`Call order placed successfully. Order ID: ${data.orderId}`, data.totalPrice, true);
        await fetchOrders();
    } catch (err) {
        console.error('Place Call Order Error:', err.message, err.stack);
        setError(`Error placing call order: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
}

async function parseVoiceOrder() {
    console.log('parseVoiceOrder called');
    const text = voiceTranscript || document.getElementById('voice-input')?.value.trim();
    if (!text) {
        console.error('No transcript provided for voice order');
        setError('Please record a voice order or enter text manually.');
        return;
    }
    if (!token) {
        console.error('No token available for voice order placement');
        setError('No token available. Please login again.');
        return;
    }
    try {
        console.log('Parsing voice order:', text);
        await fetchMenuItemsForOrder();
        if (!menuItems.length) {
            console.error('No menu items available');
            setError('No menu items available to validate voice order.');
            return;
        }
        console.log('Menu Items:', JSON.stringify(menuItems, null, 2));

        let data;
        try {
            console.log('Sending POST to /parse_order with menu items');
            const response = await fetch(aiVoiceUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    transcript: text,
                    menuItems: menuItems.map(m => ({ id: m.id, name: m.name, price: m.price }))
                })
            });
            console.log(`Voice API Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
            const responseText = await response.text();
            console.log(`Voice API Raw Response: ${responseText || 'Empty response'}`);
            if (!response.ok) {
                let errorMessage = responseText;
                try {
                    const errorData = JSON.parse(responseText);
                    errorMessage = errorData.error || responseText;
                } catch (jsonError) {
                    console.warn('Voice API response is not JSON:', responseText);
                }
                throw new Error(`Parse voice order failed: ${response.status} - ${errorMessage || 'No error details'}`);
            }
            try {
                data = JSON.parse(responseText);
                console.log('AI Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse voice API response:', jsonError.message, 'Raw Response:', responseText);
                setError(`Parse voice order failed: Invalid server response. Please try again.`, null, false);
                return;
            }
        } catch (fetchError) {
            console.error('Voice API fetch error:', fetchError.message, fetchError.stack);
            setError(`Voice API request failed: ${fetchError.message}`);
            return;
        }

        if (data.error) {
            console.error('Voice API returned error:', data.error);
            setError(`Voice API error: ${data.error}`);
            return;
        }

        if (!data.items || !Array.isArray(data.items) || data.items.length === 0) {
            console.error('No items matched in voice order:', text);
            setError(`No items matched in voice order: "${text}". Please try rephrasing (e.g., "two Zinger Burgers and one Fries") or check available menu items: ${menuItems.map(m => m.name).join(', ')}`);
            return;
        }

        const validItems = [];
        const processedItems = new Set();
        for (const item of data.items) {
            const menuItem = menuItems.find(m =>
                (item.menuItemId && m.id === item.menuItemId) ||
                (item.name && m.name.toLowerCase() === item.name.toLowerCase())
            );
            if (!menuItem) {
                console.warn(`Invalid menu item: ${item.name || 'Unknown'} (ID: ${item.menuItemId || 'N/A'}) not found`);
                continue;
            }
            if (typeof menuItem.price !== 'number' || isNaN(menuItem.price) || menuItem.price <= 0) {
                console.warn(`Invalid price for menu item: ${JSON.stringify(menuItem)}`);
                continue;
            }
            const itemKey = menuItem.id;
            if (!processedItems.has(itemKey)) {
                validItems.push({
                    menuItemId: menuItem.id,
                    name: menuItem.name,
                    quantity: parseInt(item.quantity) || 1,
                    price: menuItem.price
                });
                processedItems.add(itemKey);
            } else {
                const existingItem = validItems.find(vi => vi.menuItemId === menuItem.id);
                existingItem.quantity += parseInt(item.quantity) || 1;
            }
        }

        if (!validItems.length) {
            console.error('No valid items found in voice order:', text);
            setError(`No valid items found in voice order: "${text}". Available items: ${menuItems.map(m => m.name).join(', ')}`);
            return;
        }

        const voiceResult = document.getElementById('voice-result');
        if (!voiceResult) {
            console.error('Voice result element not found');
            setError('Voice result element not found.');
            return;
        }
        voiceResult.innerHTML = validItems.map(item => `${item.quantity} x ${item.name} ($${item.price.toFixed(2)})`).join('<br>');

        if (role === 'Customer') {
            let customerName = prompt('Enter customer name:')?.trim();
            let customerPhone = prompt('Enter customer phone:')?.trim();
            if (!customerName || !customerPhone) {
                customerName = customerName || 'Unknown Customer';
                customerPhone = customerPhone || '0000000000';
                console.warn('Customer name or phone was cancelled; using defaults:', { customerName, customerPhone });
            }
            const totalPrice = validItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
            if (isNaN(totalPrice)) {
                console.error('Total price calculation failed:', validItems);
                setError('Error calculating total price for voice order.');
                return;
            }
            const order = {
                order: {
                    customerName,
                    customerPhone,
                    totalPrice,
                    status: 'Received',
                    orderDate: new Date().toISOString().split('.')[0] + 'Z',
                    items: validItems
                }
            };
            console.log('Voice Order Payload:', JSON.stringify(order, null, 2));

            const orderResponse = await fetch(`${apiBaseUrl}/Order`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(order)
            });
            console.log(`Order API Response Status: ${orderResponse.status}, Headers: ${JSON.stringify([...orderResponse.headers])}`);
            const orderResponseText = await orderResponse.text();
            console.log(`Order API Raw Response: ${orderResponseText || 'Empty response'}`);
            let orderData = {};
            if (orderResponseText) {
                try {
                    orderData = JSON.parse(orderResponseText);
                    console.log('Order Response:', JSON.stringify(orderData, null, 2));
                } catch (jsonError) {
                    console.error('Failed to parse order response:', jsonError.message, 'Raw Response:', orderResponseText);
                    setError(`Voice order placement failed: Invalid server response. Please try again.`, null, false);
                    return;
                }
            }
            if (orderResponse.status === 401) {
                console.error('Authentication failed for voice order');
                setError('Authentication failed: Please log in again.');
                localStorage.clear();
                updateUI();
                return;
            }
            if (!orderResponse.ok) {
                console.error('Voice order placement failed:', orderResponse.status, orderData.error || orderResponseText);
                setError(`Voice order placement failed: ${orderData.error || orderResponseText || 'No error details'}`);
                return;
            }
            setError(`Voice order placed successfully. Order ID: ${orderData.orderId}`, orderData.totalPrice, true);
            await fetchOrders();
        } else {
            setError('Order parsed. Login as Customer to place order.');
        }
    } catch (err) {
        console.error('Parse Voice Order Error:', err.message, err.stack);
        setError(`Error parsing voice order: ${err.message}. Try rephrasing your order (e.g., "two Zinger Burgers and one Fries") or use manual order placement.`);
    }
}

async function submitFeedback() {
    try {
        const orderId = document.getElementById('feedback-order-id')?.value;
        const rating = document.getElementById('feedback-rating')?.value;
        const comment = document.getElementById('feedback-comment')?.value?.trim();
        if (!orderId || !rating || !comment) {
            setError('Please fill all feedback fields.');
            return;
        }
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const orderResponse = await fetch(`${apiBaseUrl}/Order`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const responseText = await orderResponse.text();
        let orders = [];
        if (responseText) {
            try {
                orders = JSON.parse(responseText);
            } catch (jsonError) {
                setError(`Feedback submission failed: Invalid server response.`);
                return;
            }
        }
        if (!orderResponse.ok) {
            setError(`Failed to validate order: ${orders.error || responseText || 'No error details'}`);
            return;
        }
        const order = orders.find(o => o.id === parseInt(orderId));
        if (!order) {
            setError(`Order ${orderId} not found.`);
            return;
        }
        if (order.status !== 'Delivered' && order.status !== 'Received') {
            setError(`Feedback can only be submitted for delivered or received orders. Current status: ${order.status}`);
            return;
        }
        let sentiment = 'neutral';
        let keywords = [];
        try {
            const aiResponse = await fetch(aiFeedbackUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ comment })
            });
            console.log(`AI Feedback Response Status: ${aiResponse.status}, Headers: ${JSON.stringify([...aiResponse.headers])}`);
            const aiResponseText = await aiResponse.text();
            console.log(`AI Feedback Raw Response: ${aiResponseText || 'Empty response'}`);
            if (aiResponse.ok && aiResponseText) {
                try {
                    const aiData = JSON.parse(aiResponseText);
                    sentiment = aiData.sentiments?.map(s => s.sentiment).join(', ') || 'neutral';
                    keywords = aiData.sentiments?.flatMap(s => s.keywords) || [];
                } catch (jsonError) {
                    console.log(`AI Feedback Parse Error: ${jsonError.message}`);
                }
            } else {
                console.log(`AI Feedback Failed: Status=${aiResponse.status}, Response=${aiResponseText}`);
            }
        } catch (aiError) {
            console.log(`AI Feedback Error: ${aiError.message}`);
        }
        const payload = {
            orderId: parseInt(orderId),
            rating: parseInt(rating),
            comment: comment,
            sentiment: sentiment,
            keywords: JSON.stringify(keywords)
        };
        console.log(`POST /api/Feedback Payload: ${JSON.stringify(payload)}`);
        const response = await fetch(`${apiBaseUrl}/Feedback`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });
        const feedbackResponseText = await response.text();
        let data = {};
        if (feedbackResponseText) {
            try {
                data = JSON.parse(feedbackResponseText);
            } catch (jsonError) {
                setError(`Feedback submission failed: Invalid server response.`);
                return;
            }
        }
        if (!response.ok) {
            setError(`Submit feedback failed: ${data.error || feedbackResponseText || 'No error details'}`);
            return;
        }
        document.getElementById('feedback-comment').value = '';
        document.getElementById('feedback-order-id').value = '1';
        document.getElementById('feedback-rating').value = '4';
        fetchFeedback();
        setError('Feedback submitted successfully.', null, true);
    } catch (err) {
        setError(`Error submitting feedback: ${err.message}`);
    }
}


async function fetchFeedback() {
    try {
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Feedback`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const responseText = await response.text();
        let feedbacks = [];
        if (responseText) {
            try {
                feedbacks = JSON.parse(responseText);
            } catch (jsonError) {
                setError(`Failed to fetch feedback: Invalid server response.`);
                return;
            }
        }
        console.log(`GET /api/Feedback Response: ${JSON.stringify(feedbacks)}`);
        if (!response.ok) {
            setError(`Failed to fetch feedback: ${feedbacks.error || responseText || 'No error details'}`);
            return;
        }
        const tbody = document.querySelector('#feedback-list tbody');
        tbody.innerHTML = '';
        feedbacks.forEach(f => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${f.orderId}</td>
                <td>${f.rating}</td>
                <td>${f.comment || 'N/A'}</td>
                <td>${f.sentiment || 'N/A'}</td>
            `;
            tbody.appendChild(row);
        });
    } catch (err) {
        setError(`Error fetching feedback: ${err.message}`);
    }
}

async function analyzeFeedback() {
    try {
        const feedbackInput = document.getElementById('feedback-input')?.value?.trim();
        if (!feedbackInput) {
            document.getElementById('feedback-result').innerHTML = '<span style="color: red;">Please enter feedback to analyze.</span>';
            return;
        }
        console.log(`AI Feedback Analysis Input: ${feedbackInput}`);
        const response = await fetch(aiFeedbackUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ comment: feedbackInput })
        });
        console.log(`AI Feedback Analysis Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`AI Feedback Analysis Raw Response: ${responseText || 'Empty response'}`);
        if (!response.ok) {
            document.getElementById('feedback-result').innerHTML = `<span style="color: red;">Failed to analyze feedback: ${responseText || 'No error details'}</span>`;
            return;
        }
        if (!responseText) {
            document.getElementById('feedback-result').innerHTML = '<span style="color: red;">No response from AI service.</span>';
            return;
        }
        let result;
        try {
            result = JSON.parse(responseText);
        } catch (jsonError) {
            document.getElementById('feedback-result').innerHTML = `<span style="color: red;">Invalid response format: ${jsonError.message}</span>`;
            return;
        }
        // Handle multiple sentiments
        let sentiments = result.sentiments || [];
        if (!Array.isArray(sentiments)) {
            sentiments = [{ sentiment: result.sentiment || 'neutral', keywords: result.keywords || [] }];
        }
        const sentimentDisplay = sentiments.map(s => {
            const sentiment = s.sentiment || 'neutral';
            const keywords = Array.isArray(s.keywords) ? s.keywords.join(', ') : 'None';
            return `<strong>Sentiment:</strong> ${sentiment}<br><strong>Keywords:</strong> ${keywords}`;
        }).join('<br><br>');
        document.getElementById('feedback-result').innerHTML = sentimentDisplay || '<span style="color: red;">No sentiments detected.</span>';
    } catch (err) {
        console.log(`AI Feedback Analysis Error: ${err.message}`);
        document.getElementById('feedback-result').innerHTML = `<span style="color: red;">Error analyzing feedback: ${err.message}</span>`;
    }
}

async function generateOrderInvoice() {
    try {
        const orderId = document.getElementById('report-order-id')?.value?.trim();
        if (!orderId) {
            setError('Please enter an order ID.');
            return;
        }
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Report/order-invoice/${orderId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log(`GET /api/Report/order-invoice/${orderId} Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        if (!response.ok) {
            const responseText = await response.text();
            let errorData = { error: responseText || 'No error details' };
            try {
                errorData = JSON.parse(responseText);
            } catch (jsonError) {
                console.log(`Parse Error: ${jsonError.message}`);
            }
            setError(`Failed to generate invoice: ${errorData.error || 'Unknown error'}`);
            return;
        }
        const contentType = response.headers.get('content-type');
        if (contentType !== 'application/pdf') {
            const responseText = await response.text();
            setError(`Invalid report format: Expected PDF, received ${contentType}`);
            console.log(`Invalid Response Content: ${responseText}`);
            return;
        }
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `order-${orderId}.pdf`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        setError('Invoice generated successfully.', null, true);
    } catch (err) {
        setError(`Error generating invoice: ${err.message}`);
        console.log(`GenerateOrderInvoice Error: ${err.message}`);
    }
}

async function generateDailySummary() {
    try {
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Report/daily-summary/pdf`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log(`GET /api/Report/daily-summary/pdf Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        if (!response.ok) {
            const responseText = await response.text();
            let errorData = { error: responseText || 'No error details' };
            try {
                errorData = JSON.parse(responseText);
            } catch (jsonError) {
                console.log(`Parse Error: ${jsonError.message}`);
            }
            setError(`Failed to generate daily summary: ${errorData.error || 'Unknown error'}`);
            return;
        }
        const contentType = response.headers.get('content-type');
        if (contentType !== 'application/pdf') {
            const responseText = await response.text();
            setError(`Invalid report format: Expected PDF, received ${contentType}`);
            console.log(`Invalid Response Content: ${responseText}`);
            return;
        }
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'daily-summary.pdf';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        setError('Daily summary generated successfully.', null, true);
    } catch (err) {
        setError(`Error generating daily summary: ${err.message}`);
        console.log(`GenerateDailySummary Error: ${err.message}`);
    }
}

async function addMenuItem() {
    const name = document.getElementById('menu-name')?.value.trim();
    const price = document.getElementById('menu-price')?.value;
    if (!name || !price) {
        console.error('Missing menu item fields:', { name, price });
        setError('Please enter item name and price.');
        return;
    }
    if (!token) {
        console.error('No token available for adding menu item');
        setError('No token available. Please login again.');
        return;
    }
    try {
        console.log('Adding menu item:', { name, price });
        const response = await fetch(`${apiBaseUrl}/Menu`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ name, price: parseFloat(price) })
        });
        console.log(`Add Menu API Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        console.log(`Add Menu API Raw Response: ${responseText || 'Empty response'}`);
        let data = {};
        if (responseText) {
            try {
                data = JSON.parse(responseText);
                console.log('Add Menu Response:', JSON.stringify(data, null, 2));
            } catch (jsonError) {
                console.error('Failed to parse add menu response:', jsonError.message, 'Raw Response:', responseText);
                setError(`Add item failed: Invalid server response.`, null, false);
                return;
            }
        }
        if (!response.ok) {
            console.error('Add menu item failed:', response.status, data.error || responseText);
            setError(`Add item failed: ${data.error || responseText || 'No error details'}`);
            return;
        }
        document.getElementById('menu-name').value = '';
        document.getElementById('menu-price').value = '';
        fetchMenu();
        setError('Menu item added successfully.', null, true);
    } catch (err) {
        console.error('Add Menu Error:', err.message, err.stack);
        setError(`Error adding menu item: ${parseErrorResponse(err, err.message.split(' - ')[1] || err.message)}`);
    }
}


async function fetchReport() {
    try {
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        const response = await fetch(`${apiBaseUrl}/Report/daily-summary`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log(`GET /api/Report/daily-summary Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        const responseText = await response.text();
        let reportData;
        try {
            reportData = JSON.parse(responseText);
        } catch (jsonError) {
            setError(`Failed to parse report: ${jsonError.message}`);
            return;
        }
        if (!response.ok) {
            setError(`Failed to fetch report: ${reportData.error || responseText || 'No error details'}`);
            return;
        }
        const reportResult = document.getElementById('report-result');
        if (reportResult) {
            reportResult.innerText = JSON.stringify(reportData, null, 2);
        }
    } catch (err) {
        setError(`Error fetching report: ${err.message}`);
    }
}


async function downloadReport(type) {
    try {
        if (!token) {
            setError('No token available. Please login again.');
            return;
        }
        let url = `${apiBaseUrl}/Report/`;
        let filename = '';
        if (type.startsWith('order-invoice/')) {
            const orderId = type.split('/')[1];
            if (!orderId || isNaN(orderId)) {
                setError('Invalid Order ID.');
                return;
            }
            url += `order-invoice/${orderId}`;
            filename = `order-${orderId}.pdf`;
        } else if (type === 'daily-summary') {
            url += 'daily-summary/pdf';
            filename = 'daily-summary.pdf';
        } else {
            setError('Invalid report type.');
            return;
        }
        console.log(`GET ${url} Request Initiated`);
        const response = await fetch(url, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log(`GET ${url} Response Status: ${response.status}, Headers: ${JSON.stringify([...response.headers])}`);
        if (!response.ok) {
            const responseText = await response.text();
            let errorData = { error: responseText || 'No error details' };
            try {
                errorData = JSON.parse(responseText);
            } catch (jsonError) {
                console.log(`Parse Error: ${jsonError.message}`);
            }
            setError(`Failed to generate report: ${errorData.error || 'Unknown error'}`);
            return;
        }
        const contentType = response.headers.get('content-type');
        if (contentType !== 'application/pdf') {
            const responseText = await response.text();
            setError(`Invalid report format: Expected PDF, received ${contentType}`);
            console.log(`Invalid Response Content: ${responseText}`);
            return;
        }
        const blob = await response.blob();
        const downloadUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(downloadUrl);
        setError('Report downloaded successfully.', null, true);
    } catch (err) {
        setError(`Error generating report: ${err.message}`);
        console.log(`DownloadReport Error: ${err.message}`);
    }
}


function startVoiceRecording() {
    console.log('Starting voice recording');
    try {
        const recognition = new (window.SpeechRecognition || window.webkitSpeechRecognition)();
        recognition.lang = 'en-US';
        recognition.interimResults = false;
        recognition.maxAlternatives = 1;

        recognition.onresult = (event) => {
            console.log('Speech recognition result received');
            voiceTranscript = event.results[0][0].transcript;
            const confidence = event.results[0][0].confidence;
            console.log('Voice Transcript:', voiceTranscript, 'Confidence:', confidence);
            document.getElementById('voice-transcript').innerText = `Transcript: ${voiceTranscript}`;
            document.getElementById('voice-confidence').innerText = `Confidence: ${(confidence * 100).toFixed(2)}%`;
            setError(confidence < 0.7 ? 'Low confidence. Try speaking clearly or use manual input.' : 'Recording successful. Click "Parse and Place Order".', null, confidence >= 0.7);
        };

        recognition.onerror = (event) => {
            console.error('Speech recognition error:', event.error);
            setError(`Voice recognition error: ${event.error}. Try manual input.`);
        };

        recognition.onend = () => {
            console.log('Speech recognition ended');
            setError('Recording stopped. Click "Parse and Place Order" or "Retry Recording".');
        };

        recognition.start();
        setError('Recording voice... Speak your order (e.g., "two zinger burgers and one fries").');
    } catch (err) {
        console.error('Voice recording error:', err.message, err.stack);
        setError(`Error starting voice recording: ${err.message}. Try manual input.`);
    }
}

function retryVoiceRecording() {
    console.log('Retrying voice recording');
    voiceTranscript = '';
    document.getElementById('voice-transcript').innerText = '';
    document.getElementById('voice-confidence').innerText = '';
    setError('Voice recording reset. Try again.');
    startVoiceRecording();
}

if (validateLocalStorage()) {
    console.log('LocalStorage before initial updateUI:', {
        token: localStorage.getItem('token') ? localStorage.getItem('token').slice(0, 10) + '...' : 'empty',
        role: localStorage.getItem('role'),
        username: localStorage.getItem('username')
    });
    setTimeout(() => {
        console.log('Delayed initial updateUI');
        updateUI();
    }, 500);
} else {
    setError('Error: LocalStorage is not accessible. Cannot initialize UI.');
}