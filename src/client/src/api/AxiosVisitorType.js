import axios from "axios";

// Get all visitor types
export const getVisitorTypes = async () => {
    try {
        const response = await axios.get('/api/visitor-types');
        return response.data;
    } catch (error) {
        console.error('Error fetching visitor types:', error);
        throw error;
    }
};

// Create a new visitor type
export const createVisitorType = async (name_visitor_type) => {
    try {
        const response = await axios.post('/api/insert-visitor-type', {
            Name: name_visitor_type
        });
        return response.data;
    } catch (error) {
        console.error('Error creating visitor type:', error);
        throw error;
    }
};

// Update a visitor type
export const updateVisitorType = async (id_visitor_type, name_visitor_type) => {
    try {
        const response = await axios.put(`/api/update-visitor-type/${id_visitor_type}`, {
            Name: name_visitor_type
        });
        return response.data;
    } catch (error) {
        console.error('Error updating visitor type:', error);
        throw error;
    }
};

// Delete a visitor type
export const deleteVisitorType = async (id_visitor_type) => {
    try {
        const response = await axios.delete(`/api/delete-visitor-type/${id_visitor_type}`);
        return response.data;
    } catch (error) {
        console.error('Error deleting visitor type:', error);
        throw error;
    }
};
