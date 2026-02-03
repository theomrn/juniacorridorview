import React, { useEffect, useState, createContext } from 'react';
import { Switch, Route, Redirect } from 'react-router-dom';
import Navbar from './component/Navbar';
import Home from './component/Home';
import TourViewer from './component/Tour';
import PanoramaViewer from './component/Pano';
import AdminTour from './component/AdminTour';
import AdminRoom from './component/AdminRoom';
import AdminRoomDetails from './component/AdminRoomDetails';
import AdminTranslation from './component/AdminTranslation';
import Login from './component/Login';
import AdminBuilding from "./component/AdminBuilding";
import AdminUser from './component/AdminUser';
import ConvertFile from './component/ConvertFile';
import AdminLanguage from './component/AdminLanguage';
import AdminVisitorType from './component/AdminVisitorType';

import './App.css';
import {Toaster} from "sonner";

export const AppContext = createContext();

const PrivateRoute = ({ component: Component, ...rest }) => {
  const isAuthenticated = localStorage.getItem("isAuthenticated") === "true";
  return (
    <Route
      {...rest}
      render={(props) =>
        isAuthenticated ? <Component {...props} /> : <Redirect to="/login" />
      }
    />
  );
};

const App = () => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [selectedImageName, setSelectedImageName] = useState('');
  const [currentRoomNumber, setCurrentRoomNumber] = useState('');
  const [selectedVisitorType, setSelectedVisitorType] = useState(null);

  useEffect(() => {
    // Check authentication status on app load
    const authStatus = localStorage.getItem("isAuthenticated") === "true";
    setIsAuthenticated(authStatus);

    // Load visitor type from localStorage if exists
    const savedVisitorType = localStorage.getItem("selectedVisitorType");
    if (savedVisitorType) {
      setSelectedVisitorType(JSON.parse(savedVisitorType));
    }
  }, []);

  return (
    <AppContext.Provider value={{ isAuthenticated, setIsAuthenticated, selectedImageName, setSelectedImageName, currentRoomNumber, setCurrentRoomNumber, selectedVisitorType, setSelectedVisitorType }}>
      <Toaster />
      <Navbar
        isAuthenticated={isAuthenticated}
        selectedImageName={selectedImageName}
        currentRoomNumber={currentRoomNumber}
      />
      <Switch>
        <Route exact path="/" component={Home} />
        <Route
          exact
          path="/pano"
          render={props => (
            <PanoramaViewer
              {...props}
              setSelectedImageName={setSelectedImageName}
              setCurrentRoomNumber={setCurrentRoomNumber}
            />
          )}
        />
        <Route exact path="/tour" component={TourViewer} />
        <PrivateRoute exact path="/admin/tour" component={AdminTour} />
        <PrivateRoute exact path="/admin/room" component={AdminRoom} />
        <PrivateRoute exact path="/admin/room/:id" component={AdminRoomDetails} />
        <PrivateRoute exact path="/admin/building" component={AdminBuilding} />
        <PrivateRoute exact path="/admin/user" component={AdminUser} />
        <PrivateRoute exact path="/admin/convert" component={ConvertFile} />
        <PrivateRoute exact path="/admin/translation" component={AdminTranslation} />
        <PrivateRoute exact path="/admin/language" component={AdminLanguage} />
        <PrivateRoute exact path="/admin/visitor-type" component={AdminVisitorType} />
        <Route exact path="/login" component={Login} />
      </Switch>
    </AppContext.Provider>
  )
}

export default App;
